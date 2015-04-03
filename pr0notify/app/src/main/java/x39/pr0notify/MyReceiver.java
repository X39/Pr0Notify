package x39.pr0notify;

import android.app.AlarmManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.SystemClock;
import android.widget.Toast;

/**
 * Created by X39 on 02.04.2015.
 */
public class MyReceiver extends BroadcastReceiver {

    @Override
    public void onReceive(Context context, Intent intent) {
        if(context == null)
            Toast.makeText(context.getApplicationContext(), "Pr0Notify - BroadcastReceiver Context null", Toast.LENGTH_LONG).show();
        switch(intent.getAction())
        {
            case "ALARM":
            case "android.intent.action.MY_PACKAGE_REPLACED":
            case "android.intent.action.BOOT_COMPLETED":
                context.startService(new Intent(context, Pr0Poller.class));
                break;
        }
    }
}
