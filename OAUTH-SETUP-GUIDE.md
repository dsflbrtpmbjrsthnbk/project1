# 🔐 OAuth Setup Guide (Google & Facebook)

The "401: invalid_client" or "OAuth client was not found" error occurs because the application is using placeholder keys. To fix this, follow these steps:

## 1. Get your API Keys

### Google
1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project.
3. Search for **APIs & Services** > **Credentials**.
4. Click **Create Credentials** > **OAuth client ID**.
5. Set **Web application** as the type.
6. Add Authorized redirect URIs: `http://localhost:5000/signin-google` (and your production URL later).
7. Copy the **Client ID** and **Client Secret**.

### Facebook
1. Go to the [Meta for Developers](https://developers.facebook.com/) portal.
2. Create a new App.
3. Add **Facebook Login** product.
4. Go to **Settings** > **Basic**.
5. Copy the **App ID** and **App Secret**.

## 2. Update appsettings.json

Open `c:\Users\Asus\Desktop\Project1\project1\appsettings.json` and add your keys:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    },
    "Facebook": {
      "AppId": "YOUR_FACEBOOK_APP_ID",
      "AppSecret": "YOUR_FACEBOOK_APP_SECRET"
    }
  }
}
```

## 3. Restart the Application

After saving the file, stop the app and run it again:
`dotnet run`

---
*Note: I have already enabled HTTP support for cookies on LocalHost, so internal Login/Register will work immediately without these keys.*
