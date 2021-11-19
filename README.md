# firebase-realtime-database-aspcore-file-provider

This repository contains the ASP.NET Core firebase real time database based file system providers for the  Syncfusion file manager component.

## Key Features

The following actions can be performed with firebase real time database based file system provider.

| **Actions** | **Description** |
| --- | --- |
| Read     | Read the files from firebase real time database. |
| Details  | Provides details about files Type, Size, Location and Modified date. |
| Download | Download the selected file or folder from the firebase real time database. |
| Upload   | Uploads a files to Firebase realtime database. It accepts uploaded media with the following characteristics: <ul><li>Maximum file size:  30MB<li>Accepted Media MIME types: `*/*` </li></ul> |
| Create   | Create a new folder. |
| Delete   | Remove a file from firebase real time database. |
| Copy     | Copy the selected Files from target. |
| Move     | Paste the copied files to the desired location. |
| Rename   | Rename a folder or file. |
| Search   | Search a file or folder in firebase real time database. |

## Prerequisites

To run the service, we need to create a [Firebase project](https://console.firebase.google.com/) to access firebase realtime database. Register the realtime database details like firebase realtime database service link, root node and service account key path in the **RegisterFirebaseRealtimeDB** method of *FilebaseFileProvider* in the controller part of the ASP.NET Core application.

```

  RegisterFirebaseRealtimeDB(string apiUrl, string rootNode, string serviceAccountKeyPath)

```

## How to run this application?

To run this application, clone the [`firebase-realtime-database-aspcore-file-provider`](https://github.com/SyncfusionExamples/firebase-realtime-database-aspcore-file-provider) repository and then navigate to its appropriate path where it has been located in your system.

To do so, open the command prompt and run the below commands one after the other.

```

git clone https://github.com/SyncfusionExamples/firebase-realtime-database-aspcore-file-provider


cd firebase-realtime-database-aspcore-file-provider

```

## Running application

Once cloned, open solution file in visual studio.Then build the project after restoring the nuget packages and run it.

## File Manager AjaxSettings

To access the basic actions such as Read, Delete, Copy, Move, Rename, Search, and Get Details of File Manager using Firebase realtime database file system service, just map the following code snippet in the Ajaxsettings property of File Manager.

Here, the `hostUrl` will be your locally hosted port number.

```
  var hostUrl = http://localhost:62870/;
  ajaxSettings: {
        url: hostUrl + 'api/FirebaseProvider/FirebaseRealtimeFileOperations'
  }
```

## File download AjaxSettings

To perform download operation, initialize the `downloadUrl` property in ajaxSettings of the File Manager component.

```
  var hostUrl = http://localhost:62870/;
  ajaxSettings: {
        url: hostUrl + 'api/FirebaseProvider/FirebaseRealtimeFileOperations',
        downloadUrl: hostUrl +'api/FirebaseProvider/FirebaseRealtimeDownload'
  }
```

## File upload AjaxSettings

To perform upload operation, initialize the `uploadUrl` property in ajaxSettings of the File Manager component.

```
  var hostUrl = http://localhost:62870/;
  ajaxSettings: {
        url: hostUrl + 'api/FirebaseProvider/FirebaseRealtimeFileOperations',
        uploadUrl: hostUrl +'api/FirebaseProvider/FirebaseRealtimeUpload'
  }
```

## File image preview AjaxSettings

To perform image preview support in the File Manager component, initialize the `getImageUrl` property in ajaxSettings of the File Manager component.

```
  var hostUrl = http://localhost:62870/;
  ajaxSettings: {
        url: hostUrl + 'api/FirebaseProvider/FirebaseRealtimeFileOperations',
        getImageUrl: hostUrl +'api/FirebaseProvider/FirebaseRealtimeGetImage'
  }
```

The FileManager will be rendered as the following.

![File Manager](https://ej2.syncfusion.com/products/images/file-manager/readme.gif)


## Support

Product support is available for through following mediums.

* Creating incident in Syncfusion [Direct-trac](https://www.syncfusion.com/support/directtrac/incidents?utm_source=npm&utm_campaign=filemanager) support system or [Community forum](https://www.syncfusion.com/forums/essential-js2?utm_source=npm&utm_campaign=filemanager).
* New [GitHub issue](https://github.com/syncfusion/ej2-javascript-ui-controls/issues/new).
* Ask your query in [Stack Overflow](https://stackoverflow.com/?utm_source=npm&utm_campaign=filemanager) with tag `syncfusion` and `ej2`.

## License

Check the license detail [here](https://github.com/syncfusion/ej2-javascript-ui-controls/blob/master/license).

## Changelog

Check the changelog [here](https://github.com/syncfusion/ej2-javascript-ui-controls/blob/master/controls/filemanager/CHANGELOG.md)

© Copyright 2020 Syncfusion, Inc. All Rights Reserved. The Syncfusion Essential Studio license and copyright applies to this distribution.
