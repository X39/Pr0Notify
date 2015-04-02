package x39.pr0notify;

import android.app.IntentService;
import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.IBinder;
import android.widget.Toast;

import org.apache.http.NameValuePair;
import org.apache.http.message.BasicNameValuePair;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.net.HttpURLConnection;
import java.net.URL;
import java.util.ArrayList;
import java.util.List;

/**
 * Created by X39 on 02.04.2015.
 */
public class Pr0Poller extends IntentService {
    private static int lastPollCount = 0;
    public static boolean doLogin(Context context, String username, String password) {
        try {
            //Create ConnectionDummy
            URL url = new URL("http://pr0gramm.com/api/user/login");
            HttpURLConnection con = (HttpURLConnection) url.openConnection();
            con.setReadTimeout(10 * 1000);
            con.setConnectTimeout(15 * 1000);
            con.setRequestMethod("POST");
            con.setDoInput(true);
            con.setDoOutput(true);

            //Add Post Parameters
            OutputStream os = con.getOutputStream();
            BufferedWriter writer = new BufferedWriter(new OutputStreamWriter(os, "UTF-8"));
            writer.write("name=" + username + "&password=" + password);
            writer.flush();
            writer.close();
            os.close();

            //Do the ACTUAL connection
            con.connect();

            //Read Response
            BufferedReader br = new BufferedReader(new InputStreamReader(con.getInputStream()));
            StringBuilder sb = new StringBuilder();
            String line;
            while ((line = br.readLine()) != null) {
                sb.append(line+"\n");
            }
            br.close();
            String responseString = sb.toString();

            //Check if we had a successful login
            String success = responseString.substring(responseString.indexOf("\"success\":") + "\"success\":".length());
            success = success.substring(0, success.indexOf(','));
            boolean tmp = Boolean.parseBoolean(success);
            if(tmp)
            {
                String headerName = null;
                String cookieString = "";
                for(int i = 1; (headerName = con.getHeaderFieldKey(i)) != null; i++)
                {
                    if(headerName.equalsIgnoreCase("set-cookie"))
                    {
                        cookieString = con.getHeaderField(i);
                        break;
                    }
                }
                if(cookieString.isEmpty())
                    return false;
                SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_ley), Context.MODE_PRIVATE);
                SharedPreferences.Editor editor = prefs.edit();
                editor.putString(context.getString(R.string.prefs_loginCookie), cookieString);
                editor.commit();
            }
            return tmp;
        }
        catch (Exception ex) {
            return false;
        }
    }
    public static int poll(Context context){
        try
        {
            //Read LastUpdateId
            SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_ley), Context.MODE_PRIVATE);
            int lastUpdateId = prefs.getInt(context.getString(R.string.prefs_lastUpdateId), 0);
            String cookie = prefs.getString(context.getString(R.string.prefs_loginCookie), "");
            if(cookie.isEmpty())
                return -1;

            //Create ConnectionDummy
            URL url = new URL("http://pr0gramm.com/api/user/sync?lastId=" + lastUpdateId);
            HttpURLConnection con = (HttpURLConnection) url.openConnection();
            con.setReadTimeout(10 * 1000);
            con.setConnectTimeout(15 * 1000);
            con.setRequestMethod("GET");
            con.setDoInput(true);
            con.setDoOutput(true);
            con.setRequestProperty("Cookie", cookie);

            //Do the ACTUAL connection
            con.connect();

            //Read Response
            BufferedReader br = new BufferedReader(new InputStreamReader(con.getInputStream()));
            StringBuilder sb = new StringBuilder();
            String line;
            while ((line = br.readLine()) != null) {
                sb.append(line+"\n");
            }
            br.close();
            String responseString = sb.toString();
            String last_sync_id_string = responseString.substring(responseString.indexOf("\"lastId\":") + "\"lastId\":".length());
            last_sync_id_string = last_sync_id_string.substring(0, last_sync_id_string.indexOf(','));
            int last_sync_id = Integer.parseInt(last_sync_id_string);
            if(last_sync_id != 0)
            {
                SharedPreferences.Editor editor = prefs.edit();
                editor.putInt(context.getString(R.string.prefs_lastUpdateId), last_sync_id);
                editor.commit();
            }
            String inboxCount_string = responseString.substring(responseString.indexOf("\"inboxCount\":") + "\"inboxCount\":".length());
            inboxCount_string = inboxCount_string.substring(0, inboxCount_string.indexOf(','));
            int inboxCount = Integer.parseInt(inboxCount_string);
            return inboxCount;
        }
        catch(Exception ex)
        {
            return -1;
        }
    }
    public  Pr0Poller()
    {
        super("Pr0Poller");
    }
    @Override
    protected void onHandleIntent(Intent workIntent)
    {
        int polled = poll(this);
        if(polled < 0)
        {
            Toast.makeText(getApplicationContext(), "Pr0Notify - Sync Failed", Toast.LENGTH_LONG).show();
            return;
        }
        //Make sure we waste no ressources on already messaged polls
        if(polled == lastPollCount)
            return;
        lastPollCount = polled;

        //Get the NotificationManager
        NotificationManager notificationManager = (NotificationManager) getSystemService(NOTIFICATION_SERVICE);

        if(polled > 0) {
            //We have new notifications so lets create/update our existing notification

            //Create the Intent for the browser action
            Intent i = new Intent(Intent.ACTION_VIEW);
            i.setData(Uri.parse("http://pr0gramm.com/inbox/unread"));
            PendingIntent pendingIntent = PendingIntent.getActivity(this, 0, i, PendingIntent.FLAG_UPDATE_CURRENT);

            //Build the Notification
            Notification.Builder builder = new Notification.Builder(this);
            builder.setDefaults(Notification.DEFAULT_SOUND | Notification.DEFAULT_VIBRATE | Notification.DEFAULT_LIGHTS);
            builder.setSmallIcon(R.mipmap.ic_launcher);
            builder.setContentText(polled == 1 ? "Neue Benachrichtigung" : "Neue Benachrichtigungen");
            builder.setNumber(polled);
            builder.setContentIntent(pendingIntent);
            builder.setLights(0xD23C22, (int)(0.5 * 1000), (int)(1 * 1000));
            builder.setVibrate(new long[]{
                    0,                      //Turn ON after
                    (long)(0.5 * 1000),     //Turn OFF after
                    (long)(0.25 * 1000),    //Turn ON after
                    (long)(0.5 * 1000),     //Turn OFF after
                    (long)(0.25 * 1000),    //Turn ON after
                    (long)(0.5 * 1000),     //Turn OFF after
                    (long)(0.25 * 1000),    //Turn ON after
                    (long)(1 * 1000)        //Turn OFF after
            });

            Notification notification = builder.build();
            notificationManager.notify(1, notification);
        }
        else {
            notificationManager.cancel(1);
        }
    }
    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }

}
