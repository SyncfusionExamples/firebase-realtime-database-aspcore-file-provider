namespace Syncfusion.EJ2.FileManager.Base
{
    public interface FirebaseRealtimeDBFileProviderBase : FileProviderBase
    {
        void RegisterFirebaseRealtimeDB(string apiUrl, string rootNode, string serviceAccountKeyPath);
    }

}
