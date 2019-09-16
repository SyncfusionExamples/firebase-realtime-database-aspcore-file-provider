namespace Syncfusion.EJ2.FileManager.Base
{
    public interface FirebaseRealtimeDBFileProviderBase : FileProviderBase
    {
        void SetRESTAPIURL(string apiURL, string rootNode, string basePath);
    }

}
