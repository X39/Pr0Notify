package x39.pr0notify;

import android.app.AlarmManager;
import android.app.Dialog;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Environment;
import android.os.StrictMode;
import android.os.SystemClock;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.util.JsonReader;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.TextView;
import android.widget.Toast;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.io.StringReader;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.UnknownHostException;
import java.util.ArrayList;
import java.util.List;

/**
 * Created by X39 on 02.04.2015.
 */

public class MainActivity extends ActionBarActivity {
    private static AlarmManager alarmMgr;
    private static PendingIntent pr0poller;

    /**
     * (re)start the Pr0Poller Intent
     * @param context a context that has access to SharedPreferences
     */
    public static void runAlarm(Context context)
    {
        //Cancel alarm when its already running
        if(alarmMgr != null)
            cancelAlarm();
        //Receive current updateInterval settings
        SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_ley), Context.MODE_PRIVATE);
        long updateInterval = prefs.getInt(context.getString(R.string.prefs_updateInterval), 5);

        //Prevent start when we have no login cookie (would be stupid then ...)
        if(prefs.getString(context.getString(R.string.prefs_loginCookie), "").isEmpty())
            return;

        //Start alarm for background polling
        alarmMgr = (AlarmManager)context.getSystemService(Context.ALARM_SERVICE);
        pr0poller = PendingIntent.getBroadcast(context, 0, new Intent(context, MyReceiver.class).setAction("ALARM"), PendingIntent.FLAG_UPDATE_CURRENT);
        alarmMgr.setRepeating(AlarmManager.ELAPSED_REALTIME, 0, updateInterval * 60 * 1000, pr0poller);
    }

    /**
     * Cancel the Pr0Poller Intent (if it is running)
     */
    public static void cancelAlarm()
    {
        if(alarmMgr != null)
            alarmMgr.cancel(pr0poller);
        alarmMgr = null;
    }



    final Context context = this;
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_ley), Context.MODE_PRIVATE);
        EditText editText = (EditText) findViewById(R.id.tb_UpdateInterval);
        int updateInterval = prefs.getInt(getString(R.string.prefs_updateInterval), 1);
        editText.setText(updateInterval + "");
        String cookie = prefs.getString(getString(R.string.prefs_loginCookie), "");
        TextView cookieTextView = (TextView) findViewById(R.id.label_cookie);
        cookieTextView.setText(cookie.isEmpty() ? "Cookie nicht gesetzt :(" : "Cookie ist gesetzt :)");
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();
        StrictMode.setThreadPolicy(policy);
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        // Inflate the menu; this adds items to the action bar if it is present.
        getMenuInflater().inflate(R.menu.menu_main, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(MenuItem item) {
        // Handle action bar item clicks here. The action bar will
        // automatically handle clicks on the Home/Up button, so long
        // as you specify a parent activity in AndroidManifest.xml.
        int id = item.getItemId();

        //noinspection SimplifiableIfStatement
        if (id == R.id.action_settings) {
            return true;
        }

        return super.onOptionsItemSelected(item);
    }
    public void onClick_btn_updateInterval(View view)
    {
        EditText editText = (EditText) findViewById(R.id.tb_UpdateInterval);
        String s = editText.getText().toString();
        int interval;
        try
        {
            interval = Integer.parseInt(s);
            if(interval < 5)
                throw new Exception("Interval muss mindestens 5 sein!");
        }
        catch (Exception ex)
        {
            Toast.makeText(getApplicationContext(), ex.getMessage(), Toast.LENGTH_LONG).show();
            return;
        }
        cancelAlarm();
        SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_ley), Context.MODE_PRIVATE);
        SharedPreferences.Editor editor = prefs.edit();
        editor.putInt(getString(R.string.prefs_updateInterval), interval);
        editor.commit();
        if(!prefs.getString(getString(R.string.prefs_loginCookie), "").isEmpty())
        {
            Intent i = new Intent(this, Pr0Poller.class);
            i.setAction("runAlarm");
            startService(i);
        }
    }
    public void onClick_btn_syncNow(View view)
    {
        Toast.makeText(getApplicationContext(), "Synchronisiere ...", Toast.LENGTH_LONG).show();
        Pr0Poller.poll(this);
    }
    public void btn_searchForUpdate_onClick(View view) throws Exception {
        //Read LastUpdateId
        SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_ley), Context.MODE_PRIVATE);
        String cookie = prefs.getString(context.getString(R.string.prefs_loginCookie), "");
        if(cookie.isEmpty())
            throw new Exception("Cookie nicht gesetzt");

        //Create ConnectionDummy
        URL url = new URL("http://x39.unitedtacticalforces.de/api.php?action=projects&project=pr0notifyAndroid");
        HttpURLConnection con = (HttpURLConnection) url.openConnection();
        con.setReadTimeout(10 * 1000);
        con.setConnectTimeout(15 * 1000);
        con.setRequestMethod("GET");
        con.setDoInput(true);
        con.setDoOutput(true);
        con.setRequestProperty("Cookie", cookie);

        //Do the ACTUAL connection
        try
        {
            con.connect();
        }
        catch(UnknownHostException ex)
        {
            Toast.makeText(getApplicationContext(), "Kein netz? Konnte verbindung nicht auflösen :(", Toast.LENGTH_LONG).show();
            return;
        }
        catch(Exception ex)
        {
            Toast.makeText(getApplicationContext(), "Failed :(\n" + ex.getMessage(), Toast.LENGTH_LONG).show();
            return;
        }
        //Read Response
        JsonReader reader = new JsonReader(new InputStreamReader(con.getInputStream()));
        boolean success = false;
        String error = "";
        String productVersion = "";
        String download = "";
        reader.beginObject();
        while(reader.hasNext())
        {
            switch(reader.nextName())
            {
                case "success":
                    success = reader.nextBoolean();
                    break;
                case "error":
                    error = reader.nextString();
                    break;
                case "content":
                    reader.beginObject();
                    while(reader.hasNext())
                    {
                        switch(reader.nextName())
                        {
                            case "version":
                                productVersion = reader.nextString();
                                break;
                            case "download":
                                download = reader.nextString();
                                break;
                            default:
                                reader.skipValue();
                                break;
                        }
                    }
                    reader.endObject();
                    break;
                default:
                    reader.skipValue();
                    break;
            }
        }
        reader.endObject();

        if(!success)
        {
            Toast.makeText(getApplicationContext(), "Failed :(\n" + error, Toast.LENGTH_LONG).show();
            return;
        }
        if(productVersion.equalsIgnoreCase(getString(R.string.productVersion)))
        {
            Toast.makeText(getApplicationContext(), "Kein Update verfügbar", Toast.LENGTH_LONG).show();
        }
        else
        {
            try
            {
                update(download);
            }
            catch (Exception ex)
            {
                Toast.makeText(getApplicationContext(), "Failed :(\n" + ex.getMessage(), Toast.LENGTH_LONG).show();
                return;
            }
        }

    }
    public void update(String sUrl) throws Exception{
        URL url = new URL(sUrl);
        HttpURLConnection con = (HttpURLConnection) url.openConnection();
        con.setRequestMethod("GET");
        con.setDoOutput(true);
        con.connect();

        File file = new File(Environment.getExternalStorageDirectory() + "/download/");
        file.mkdirs();
        File outputFile = new File(file, "update.apk");
        FileOutputStream fos = new FileOutputStream(outputFile);

        InputStream inputStream = con.getInputStream();

        byte[] buffer = new byte[1024];
        int i = 0;
        while ((i = inputStream.read(buffer)) != -1) {fos.write(buffer, 0, i);}
        fos.close();
        inputStream.close();

        Intent intent = new Intent(Intent.ACTION_VIEW);
        intent.setDataAndType(Uri.fromFile(new File(Environment.getExternalStorageDirectory() + "/download/" + "update.apk")), "application/vnd.android.package-archive");
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
        startActivity(intent);
    }
    public void onClick_btn_setLogin(View view)
    {
        final Dialog dialog = new Dialog(context);
        dialog.setContentView(R.layout.logindialog);
        dialog.setTitle("Pr0gramm Login");

        Button dialogButton = (Button) dialog.findViewById(R.id.btn_doLogin);
        dialogButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                EditText et_username = (EditText) dialog.findViewById(R.id.tb_username);
                EditText et_password = (EditText) dialog.findViewById(R.id.tb_password);
                String username = et_username.getText().toString();
                String password = et_password.getText().toString();
                if(username.isEmpty() || password.isEmpty())
                {
                    Toast.makeText(getApplicationContext(), "Ohne daten kein Login", Toast.LENGTH_LONG).show();
                    return;
                }
                StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();
                StrictMode.setThreadPolicy(policy);
                if(Pr0Poller.doLogin(context, username, password))
                {
                    Toast.makeText(getApplicationContext(), "Login Erfolgreich :)", Toast.LENGTH_LONG).show();
                    SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_ley), Context.MODE_PRIVATE);
                    String cookie = prefs.getString(getString(R.string.prefs_loginCookie), "");
                    TextView cookieTextView = (TextView) findViewById(R.id.label_cookie);
                    cookieTextView.setText(cookie.isEmpty() ? "Cookie nicht gesetzt :(" : "Cookie ist gesetzt :)");
                    Intent i = new Intent(context, Pr0Poller.class);
                    i.setAction("runAlarm");
                    startService(i);
                    dialog.dismiss();
                }
                else
                    Toast.makeText(getApplicationContext(), "Login Fehlgeschlagen :(", Toast.LENGTH_LONG).show();
            }
        });
        dialog.show();
    }
}
