package com.opensekai;

import android.app.Activity;
import android.content.Intent;
import android.net.Uri;
import android.os.Handler;
import android.os.Looper;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileInputStream;
import java.io.OutputStream;

public class ShareExportHelper {
    private static final int REQUEST_CODE_SAVE = 2001;
    private static String pendingSourcePath;
    private static String pendingCallbackObject;
    private static String pendingCallbackMethod;
    private static Handler mainHandler = new Handler(Looper.getMainLooper());

    public static void SaveAndShare(final String sourcePath, final String callbackObject, final String callbackMethod) {
        pendingSourcePath = sourcePath;
        pendingCallbackObject = callbackObject;
        pendingCallbackMethod = callbackMethod;

        mainHandler.post(new Runnable() {
            @Override
            public void run() {
                Activity activity = UnityPlayer.currentActivity;
                Intent intent = new Intent(Intent.ACTION_CREATE_DOCUMENT);
                intent.addCategory(Intent.CATEGORY_OPENABLE);
                intent.setType("application/zip");
                intent.putExtra(Intent.EXTRA_TITLE, new File(sourcePath).getName());
                activity.startActivityForResult(intent, REQUEST_CODE_SAVE);
            }
        });
    }

    public static boolean OnActivityResult(int requestCode, int resultCode, Intent data) {
        if (requestCode == REQUEST_CODE_SAVE) {
            if (resultCode == Activity.RESULT_OK && data != null) {
                Uri uri = data.getData();
                if (uri != null && pendingSourcePath != null) {
                    try {
                        copyFileToUri(pendingSourcePath, uri);
                        sendCallback("success");
                        return true;
                    } catch (Exception e) {
                        sendCallback("error:" + e.getMessage());
                        return true;
                    }
                }
            } else {
                sendCallback("cancelled");
                return true;
            }
        }
        return false;
    }

    private static void sendCallback(String message) {
        if (pendingCallbackObject != null && pendingCallbackMethod != null) {
            final String obj = pendingCallbackObject;
            final String meth = pendingCallbackMethod;
            mainHandler.post(new Runnable() {
                @Override
                public void run() {
                    UnityPlayer.UnitySendMessage(obj, meth, message);
                }
            });
        }
        pendingSourcePath = null;
        pendingCallbackObject = null;
        pendingCallbackMethod = null;
    }

    private static void copyFileToUri(String sourcePath, Uri destUri) throws Exception {
        File sourceFile = new File(sourcePath);
        Activity activity = UnityPlayer.currentActivity;
        OutputStream outputStream = activity.getContentResolver().openOutputStream(destUri);
        if (outputStream == null) {
            throw new Exception("Failed to open output stream");
        }
        FileInputStream inputStream = new FileInputStream(sourceFile);
        byte[] buffer = new byte[8192];
        int length;
        while ((length = inputStream.read(buffer)) > 0) {
            outputStream.write(buffer, 0, length);
        }
        inputStream.close();
        outputStream.close();
    }
}
