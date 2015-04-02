package x39.pr0notify;

import android.app.AlarmManager;
import android.app.Dialog;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.StrictMode;
import android.os.SystemClock;
import android.support.v7.app.ActionBarActivity;
import android.os.Bundle;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

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
        long updateInterval = prefs.getInt(context.getString(R.string.prefs_updateInterval), 1);

        //Prevent start when we have no login cookie (would be stupid then ...)
        if(prefs.getString(context.getString(R.string.prefs_loginCookie), "").isEmpty())
            return;

        //Start alarm for background polling
        alarmMgr = (AlarmManager)context.getSystemService(Context.ALARM_SERVICE);
        pr0poller = PendingIntent.getBroadcast(context, 0, new Intent(context, Pr0Poller.class).setAction("ALARM"), 0);
        alarmMgr.setRepeating(AlarmManager.ELAPSED_REALTIME, SystemClock.elapsedRealtime() + 3000, updateInterval * 1000, pr0poller);
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
        EditText editText = (EditText) findViewById(R.id.tb_UpdateInterval);
        editText.setText("1");

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
            if(interval <= 0)
                throw new Exception("Interval muss > 0 sein!");
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
            runAlarm(this.context);
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
                    runAlarm(context);
                    dialog.dismiss();
                }
                else
                    Toast.makeText(getApplicationContext(), "Login Fehlgeschlagen :(", Toast.LENGTH_LONG).show();
            }
        });
        dialog.show();
    }
}
