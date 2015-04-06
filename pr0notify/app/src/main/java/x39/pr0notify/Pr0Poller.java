package x39.pr0notify;

import android.app.IntentService;
import android.app.Notification;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.net.Uri;
import android.os.HandlerThread;
import android.os.IBinder;
import android.os.StrictMode;
import android.util.Log;
import android.widget.Toast;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.InputStreamReader;
import java.io.OutputStream;
import java.io.OutputStreamWriter;
import java.net.HttpURLConnection;
import java.net.URL;
import java.net.UnknownHostException;
import java.util.ArrayList;
import java.util.List;

/**
 * Created by X39 on 02.04.2015.
 */
public class Pr0Poller extends Service{
    @Override
    public IBinder onBind(Intent arg0) {
        return null;
    }

    @Override
    public void onCreate() {
        MainActivity.runAlarm(this);
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();
        StrictMode.setThreadPolicy(policy);
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        poll(this);
        return START_NOT_STICKY;
    }

    @Override
    public void onDestroy() { }
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
    private static int pollData(Context context) throws Exception{
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
    private static Pr0Message[] pollMessages(Context context) throws Exception{
        //Read LastUpdateId
        SharedPreferences prefs = context.getSharedPreferences(context.getString(R.string.preference_file_ley), Context.MODE_PRIVATE);
        String cookie = prefs.getString(context.getString(R.string.prefs_loginCookie), "");
        if(cookie.isEmpty())
            throw new Exception("Cookie nicht gesetzt");

        //Create ConnectionDummy
        URL url = new URL("http://pr0gramm.com/api/inbox/all");
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
        List<Pr0Message> list = new ArrayList<Pr0Message>();
        responseString = responseString.substring(responseString.indexOf("{\"messages\":["));
        responseString = responseString.substring(0, responseString.indexOf("],\"hasOlder\":"));
        String[] responseArray = responseString.split("\"},{\"id\":");
        for(int i = 0; i < responseArray.length; i++)
            responseArray[i] = "{\"id\":" + responseArray[i] + "}";
        for(String s : responseArray)
            list.add(Pr0Message.parse(s));
        return (Pr0Message[])list.toArray();
    }
    private static void poll(Context context) {
        int polled;
        try
        {
            polled = pollData(context);
        }
        catch(UnknownHostException ex)
        {
            return;
        }
        catch (Exception ex)
        {
            Toast.makeText(context.getApplicationContext(), "Pr0Notify - Sync Failed - " + ex.getMessage(), Toast.LENGTH_LONG).show();
            Log.e("Pr0Notify", "NPE", ex);
            return;
        }
        if(polled < 0)
        {
            Toast.makeText(context.getApplicationContext(), "Pr0Notify - Sync Failed", Toast.LENGTH_LONG).show();
            return;
        }
        //Make sure we waste no ressources on already messaged polls
        if(polled == lastPollCount)
            return;
        lastPollCount = polled;

        //Get the NotificationManager
        NotificationManager notificationManager = (NotificationManager) context.getSystemService(context.NOTIFICATION_SERVICE);

        if(polled > 0) {
            //We have new notifications so lets create/update our existing notification

            //Create the Intent for the browser action
            Intent i = new Intent(Intent.ACTION_VIEW);
            i.setData(Uri.parse("http://pr0gramm.com/inbox/unread"));
            PendingIntent pendingIntent = PendingIntent.getActivity(context, 0, i, PendingIntent.FLAG_UPDATE_CURRENT);

            //Build the Notification
            Notification.Builder builder = new Notification.Builder(context);
            builder.setDefaults(Notification.DEFAULT_SOUND | Notification.DEFAULT_VIBRATE | Notification.DEFAULT_LIGHTS);
            builder.setSmallIcon(R.mipmap.ic_launcher);
            builder.setLargeIcon(BitmapFactory.decodeResource(context.getResources(), R.raw.logo));
            builder.setContentTitle(polled == 1 ? "Neue Benachrichtigung" : "Neue Benachrichtigungen");
            if(polled == 1)
            {
                try
                {
                    Pr0Message[] messages = pollMessages(context);
                    builder.setContentText(messages[0].message.length() > 128 ? messages[0].message.substring(0, 128 - 4) + " ..." : messages[0].message);
                    builder.setContentInfo(messages[0].name);
                }
                catch (Exception ex)
                {
                    Toast.makeText(context.getApplicationContext(), "Pr0Notify - Sync Failed - " + ex.getMessage(), Toast.LENGTH_LONG).show();
                    Log.e("Pr0Notify", ex.getMessage(), ex);
                    return;
                }

            }
            builder.setNumber(polled);
            builder.setContentIntent(pendingIntent);
            builder.setAutoCancel(true);
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
}
