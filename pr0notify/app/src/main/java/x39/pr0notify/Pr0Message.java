package x39.pr0notify;

import android.util.JsonReader;
import android.util.JsonToken;

import java.io.StringReader;

/**
 * Created by X39 on 02.04.2015.
 */
public class Pr0Message {
    public long id;
    public long imageId;
    public String thumb;
    public String name;
    public String mark;
    public long senderId;
    public String score;
    public long created;
    public String message;

    public boolean isPrivateMessage() {
        return this.imageId == 0;
    }
    public static Pr0Message parse(JsonReader reader) throws Exception{
        reader.beginObject();
        Pr0Message msg = new Pr0Message();
        while(reader.hasNext())
        {
            if(reader.peek() == JsonToken.NULL)
            {
                reader.skipValue();
                continue;
            }
            try {
                switch (reader.nextName()) {
                    case "id":
                        msg.id = reader.nextLong();
                        break;
                    case "itemId":
                        msg.imageId = reader.nextLong();
                        break;
                    case "thumb":
                        msg.thumb = reader.nextString();
                        break;
                    case "name":
                        msg.name = reader.nextString();
                        break;
                    case "mark":
                        msg.mark = reader.nextString();
                        break;
                    case "senderId":
                        msg.senderId = reader.nextLong();
                        break;
                    case "score":
                        msg.score = reader.nextString();
                        break;
                    case "created":
                        msg.created = reader.nextLong();
                        break;
                    case "message":
                        msg.message = reader.nextString();
                        break;
                    default:
                        reader.skipValue();
                        break;
                }
            }
            catch (Exception ex)
            {
                reader.skipValue();
            }
        }
        reader.endObject();
        return msg;
    }
}
