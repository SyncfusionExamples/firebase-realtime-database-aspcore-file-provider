using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Syncfusion.EJ2.FileManager.Base;
using FirebaseHelper;

namespace Syncfusion.EJ2.FileManager.FirebaseRealTimeDBFileProvider
{
    public class FirebaseRealTimeDBFileProvider : FirebaseRealTimeDBFileProviderBase
    {
        protected string filterPath = null;
        protected string filterId = null;
        protected long fileData;
        FirebaseOperations firebaseAPI;
        FirebaseOperations getFirebaseRootNode;
        FirebaseResponse getResponse;
        FileManagerDirectoryContent[] firebaseGetData;
        private FileStreamResult fileStreamResult;
        List<FileManagerDirectoryContent> copyFiles = new List<FileManagerDirectoryContent>();
        protected string APIURL;
        protected string rootNode;

        // Registering the firebase realtime database storage 
        public void SetRESTAPIURL(string APIURL, string rootNode)
        {
            this.APIURL = APIURL;
            this.rootNode = rootNode;
            this.UpdateFirebaseJSONData();
        }

        public FirebaseRealTimeDBFileProvider()
        {
        }
        //updates the firebase realtime database json
        private void UpdateFirebaseJSONData()
        {
            this.firebaseAPI = new FirebaseOperations(this.APIURL);
            this.getFirebaseRootNode = firebaseAPI.Node(this.rootNode);
            this.getResponse = getFirebaseRootNode.Get(this.APIURL + "/" + this.rootNode + "/");
            dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(getResponse.JSONContent);
            this.firebaseGetData = JsonConvert.DeserializeObject<FileManagerDirectoryContent[]>(getResponse.JSONContent);
            this.firebaseGetData = this.firebaseGetData.Where(c => c != null).ToArray();
        }

        // Reads the files within the directorty
        public FileManagerResponse GetFiles(string path, bool showHiddenItems, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse readResponse = new FileManagerResponse();
            try
            {
                if (path == null)
                {
                    path = string.Empty;
                }
                List<FileManagerDirectoryContent> cwd = new List<FileManagerDirectoryContent>();
                if (path == "/")
                {
                    cwd = firebaseGetData.Where(x => x.Id == "0").Select(x => new FileManagerDirectoryContent()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Size = x.Size,
                        DateCreated = x.DateCreated,
                        DateModified = x.DateModified,
                        Type = System.IO.Path.GetExtension(x.Name),
                        HasChild = x.HasChild,
                        FilterPath = x.FilterPath,
                        ParentId = "",
                        FilterId = x.FilterId,
                        IsFile = x.IsFile,
                    }).ToList();
                }
                else
                {
                    cwd = firebaseGetData.Where(x => x.Id == data[0].Id).Select(x => new FileManagerDirectoryContent()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Size = x.Size,
                        DateCreated = x.DateCreated,
                        DateModified = x.DateModified,
                        Type = System.IO.Path.GetExtension(x.Name),
                        HasChild = x.HasChild,
                        ParentId = x.ParentId,
                        FilterPath = x.FilterPath,
                        FilterId = x.FilterId,
                        IsFile = x.IsFile,
                    }).ToList();

                }
                FileManagerDirectoryContent[] files = JsonConvert.DeserializeObject<FileManagerDirectoryContent[]>(getResponse.JSONContent);
                if (path == "/")
                {
                    files = firebaseGetData.Where(x => x.isRoot == true).Select(x => new FileManagerDirectoryContent()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Size = x.Size,
                        DateCreated = x.DateCreated,
                        DateModified = x.DateModified,
                        Type = System.IO.Path.GetExtension(x.Name),
                        HasChild = x.HasChild,
                        FilterPath = x.FilterPath,
                        ParentId = "0",
                        FilterId = x.FilterId,
                        IsFile = x.IsFile,
                    }).ToArray();
                }
                else
                {
                    files = firebaseGetData.Where(x => x.ParentId == data[0].Id).Select(x => new FileManagerDirectoryContent()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Size = x.Size,
                        DateCreated = x.DateCreated,
                        DateModified = x.DateModified,
                        Type = x.Type,
                        ParentId = data[0].Id,
                        HasChild = x.HasChild,
                        FilterPath = x.FilterPath,
                        FilterId = x.FilterId,
                        IsFile = x.IsFile,
                    }).ToArray();
                }
                readResponse.Files = files;
                readResponse.CWD = cwd[0];
                return readResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "404";
                er.Message = e.Message.ToString();
                readResponse.Error = er;
                return readResponse;
            }
        }

        // Creates a newFolder
        public FileManagerResponse Create(string path, string name, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse createResponse = new FileManagerResponse();
            try
            {
                int idValue = this.firebaseGetData.Select(x => x.Id).ToArray().Select(int.Parse).ToArray().Max();
                this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + data[0].Id, JsonConvert.SerializeObject(new UpdateChild() { hasChild = true, dateModified = DateTime.Now.ToString() }));
                FileManagerDirectoryContent CreateData = new FileManagerDirectoryContent()
                {
                    Id = (idValue + 1).ToString(),
                    Name = name,
                    Size = 0,
                    DateCreated = DateTime.Now.ToString(),
                    DateModified = DateTime.Now.ToString(),
                    Type = "folder",
                    HasChild = false,
                    ParentId = data[0].Id,
                    IsFile = false,
                    isRoot = (Int32.Parse(data[0].Id) == 0) ? true : false,
                    FilterPath = this.GetRelativePath(data[0].Id, "/").Substring(this.rootNode.Length) + "/",
                    FilterId = this.GetRelativeId(data[0].Id) + "/"
                };
                this.updateDBNode(CreateData, idValue);
                createResponse.Files = new FileManagerDirectoryContent[] { CreateData };
                return createResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "404";
                er.Message = e.Message.ToString();
                createResponse.Error = er;
                return createResponse;
            }
        }
        // Gets the details of the selected item(s).
        public FileManagerResponse Details(string path, string[] names, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse getDetailResponse = new FileManagerResponse();
            FileDetails detailFiles = new FileDetails();
            FileDetails fileDetails = new FileDetails();
            try
            {
                if (names.Length == 0 || names.Length == 1)
                {
                    FileManagerDirectoryContent[] cwd = firebaseGetData.Where(x => x.Id == data[0].Id).Select(x => new FileManagerDirectoryContent()
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Size = x.Size,
                        DateCreated = x.DateCreated,
                        DateModified = x.DateModified,
                        Type = x.Type,
                        HasChild = x.HasChild,
                        FilterPath = x.FilterPath,
                        FilterId = x.FilterId,
                        IsFile = x.IsFile,
                    }).ToArray();
                    detailFiles = new FileDetails()
                    {
                        Name = cwd[0].Name,
                        MultipleFiles = false,
                        IsFile = cwd[0].IsFile,
                        Location = cwd[0].Path,
                        Size = cwd[0].Size.ToString(),
                        Created = cwd[0].DateCreated,
                        Modified = cwd[0].DateModified
                    };
                    detailFiles.Size = byteConversion(long.Parse("" + this.GetItemSize(data))).ToString();
                    detailFiles.Location = this.GetRelativePath(cwd[0].Id, "\\");
                }
                else
                {
                    List<string> NamesList = new List<string>();
                    for (int i = 0; i < names.Length; i++)
                    {
                        FileManagerDirectoryContent[] cwd = firebaseGetData.Where(x => x.Id == data[i].Id).Select(x => new FileManagerDirectoryContent()
                        {
                            Id = x.Id,
                            Name = x.Name,
                            Size = x.Size,
                            DateCreated = x.DateCreated,
                            DateModified = x.DateModified,
                            Type = x.Type,
                            HasChild = x.HasChild,
                            FilterPath = x.FilterPath,
                            FilterId = x.FilterId,
                            IsFile = x.IsFile,
                        }).ToArray();
                        NamesList.Add(cwd[0].Name);
                    }
                    fileDetails.Name = string.Join(", ", NamesList.ToArray());
                    fileDetails.Location = "Various folders";
                    fileDetails.Size = byteConversion(long.Parse("" + this.GetItemSize(data))).ToString();
                    fileDetails.MultipleFiles = true;
                    detailFiles = fileDetails;
                }
                getDetailResponse.Details = detailFiles;
                this.fileData = 0;
                return getDetailResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "404";
                er.Message = e.ToString();
                getDetailResponse.Error = er;
                return getDetailResponse;
            }
        }

        // Gets relative path of file or folder
        public string GetRelativePath(string y, string pathSymbol)
        {
            FileManagerDirectoryContent[] file = firebaseGetData.Where(x => x.Id == y).Select(x => x).ToArray();

            if (String.IsNullOrEmpty(this.filterPath))
            {
                this.filterPath = file[0].Name;
            }
            else
            {
                this.filterPath = file[0].Name + this.filterPath;
            }
            if (!String.IsNullOrEmpty(file[0].ParentId))
            {
                if (!file[0].isRoot)
                {
                    this.filterPath = pathSymbol + filterPath;
                    this.GetRelativePath(file[0].ParentId, pathSymbol);
                }
                else
                {
                    FileManagerDirectoryContent[] path = firebaseGetData.Where(x => x.Id == file[0].ParentId).Select(x => x).ToArray();
                    this.filterPath = path[0].Name + pathSymbol + filterPath;
                }
            }
            return this.filterPath;
        }

        // Gets relative Id of file or folder
        public string GetRelativeId(string y)
        {
            FileManagerDirectoryContent[] file = firebaseGetData.Where(x => x.Id == y).Select(x => x).ToArray();

            if (String.IsNullOrEmpty(this.filterId))
            {
                this.filterId = file[0].Id;
            }
            else
            {
                this.filterId = file[0].Id + this.filterId;
            }
            bool isRoot = file[0].isRoot;
            if (!String.IsNullOrEmpty(file[0].ParentId))
            {
                if (!isRoot)
                {
                    this.filterId = "/" + filterId;
                    this.GetRelativeId(file[0].ParentId);
                }
                else
                {
                    FileManagerDirectoryContent[] path = firebaseGetData.Where(x => x.Id == file[0].ParentId).Select(x => x).ToArray();
                    this.filterId = path[0].Id + "/" + filterId;
                }
            }
            return this.filterId;
        }

        // Returns the size of the selected file or folder
        public long GetItemSize(FileManagerDirectoryContent[] data)
        {
            foreach (FileManagerDirectoryContent item in data)
            {
                this.fileData = this.fileData + item.Size;
                long[] fileData = this.firebaseGetData.Where(x => x.ParentId == item.Id).Select(x => x.Size).ToArray();
                this.fileData = this.fileData + fileData.Sum();
                FileManagerDirectoryContent[] dataFile = this.firebaseGetData.Where(x => x.ParentId == item.Id && x.IsFile == false).Select(x => x).ToArray();
                if (dataFile.Length > 0)
                {
                    this.GetItemSize(dataFile);
                }
            }
            return this.fileData;
        }
        //Deletes the child nodes from a folder
        public void DeleteItems(string item)
        {
            FileManagerDirectoryContent[] childs = this.firebaseGetData.Where(x => x.ParentId == item).Select(x => x).ToArray();
            this.getFirebaseRootNode.Delete(this.APIURL + "/" + this.rootNode + "/" + item);
            if (childs.Length != 0)
            {
                foreach (FileManagerDirectoryContent child in childs)
                {
                    this.DeleteItems(child.Id);
                }
            }
        }
        // Deletes file(s) or folder(s).
        public virtual FileManagerResponse Delete(string path, string[] names, params FileManagerDirectoryContent[] data)
        {
            FileManagerDirectoryContent[] removedFiles = new FileManagerDirectoryContent[names.Length];
            removedFiles = data.Select(x => new FileManagerDirectoryContent()
            {
                Id = x.Id,
                Name = x.Name,
                Size = x.Size,
                DateCreated = x.DateCreated,
                DateModified = x.DateModified,
                Type = x.Type,
                HasChild = x.HasChild,
                FilterPath = x.FilterPath,
                FilterId = x.FilterId,
                IsFile = x.IsFile,
            }).ToArray();
            foreach (FileManagerDirectoryContent item in data)
            {
                this.DeleteItems(item.Id);
            }
            FileManagerResponse DeleteResponse = new FileManagerResponse();
            try
            {
                this.UpdateFirebaseJSONData();
                FileManagerDirectoryContent[] emptyFolder = this.firebaseGetData.Where(x => x.ParentId == data[0].ParentId).Select(x => x).ToArray();
                if (emptyFolder.Length == 0)
                {
                    this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + data[0].ParentId, JsonConvert.SerializeObject(new UpdateChild() { hasChild = false, dateModified = DateTime.Now.ToString() }));
                }
                DeleteResponse.Files = removedFiles;
                return DeleteResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "404";
                er.Message = e.Message.ToString();
                DeleteResponse.Error = er;

                return DeleteResponse;
            }
        }
        // Returns the last node id from the firebase database
        public int updatedNodeId()
        {
            this.UpdateFirebaseJSONData();
            return this.firebaseGetData.Select(x => x.Id).ToArray().Select(int.Parse).ToArray().Max();
        }

        // Renames file(s) or folder(s).
        public FileManagerResponse Rename(string path, string name, string newName, bool replace = false, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse renameResponse = new FileManagerResponse();
            try
            {
                this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + data[0].Id, JsonConvert.SerializeObject(new RenameNode() { name = "" + newName, type = System.IO.Path.GetExtension(newName) }));
                this.UpdateFirebaseJSONData();
                FileManagerDirectoryContent[] renamedData = firebaseGetData.Where(x => x.Id == data[0].Id).Select(x => new FileManagerDirectoryContent()
                {
                    Id = x.Id,
                    Name = newName,
                    Size = x.Size,
                    DateCreated = x.DateCreated,
                    DateModified = (System.IO.Path.GetExtension(newName) == "folder") ? DateTime.Now.ToString() : x.DateModified,
                    Type = System.IO.Path.GetExtension(newName),
                    HasChild = x.HasChild,
                    FilterPath = x.FilterPath,
                    FilterId = x.FilterId,
                    IsFile = x.IsFile,
                }).ToArray();
                renameResponse.Files = renamedData;
                return renameResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = er.Message.Contains("Access is denied") ? "401" : "417";
                renameResponse.Error = er;

                return renameResponse;
            }
        }

        //Copies the child folder or files from a parent node
        public void copyFolderItems(FileManagerDirectoryContent item, FileManagerDirectoryContent target)
        {
            if (!item.IsFile)
            {
                int idVal = this.updatedNodeId();
                List<FileManagerDirectoryContent> i = this.firebaseGetData.Where(x => x.Id == item.Id).Select(x => x).ToList();
                FileManagerDirectoryContent CreateData = new FileManagerDirectoryContent()
                {
                    Id = (idVal + 1).ToString(),
                    Name = item.Name,
                    Size = 0,
                    DateCreated = DateTime.Now.ToString(),
                    DateModified = DateTime.Now.ToString(),
                    Type = "folder",
                    HasChild = false,
                    ParentId = target.Id,
                    IsFile = false,
                    Content = i[0].Content,
                    isRoot = String.IsNullOrEmpty(target.ParentId) ? true : false,
                    FilterPath = this.GetRelativePath(target.Id, "/").Substring(this.rootNode.Length) + "/",
                    FilterId = this.GetRelativeId(target.Id.ToString()) + "/"
                };
                copyFiles.Add(CreateData);
                this.updateDBNode(CreateData, idVal);
                if (target.HasChild == false)
                {
                    this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + target.Id, JsonConvert.SerializeObject(new UpdateChild() { hasChild = true, dateModified = DateTime.Now.ToString() }));
                }
            }
            this.filterPath = this.filterId = null;
            FileManagerDirectoryContent[] childs = this.firebaseGetData.Where(x => x.ParentId == item.Id).Select(x => x).ToArray();
            int idValue = this.updatedNodeId();
            if (childs.Length > 0)
            {
                foreach (FileManagerDirectoryContent child in childs)
                {
                    this.filterPath = this.filterId = null;
                    if (child.IsFile)
                    {
                        int idVal = this.updatedNodeId();
                        // Copy the file
                        FileManagerDirectoryContent CreateData = new FileManagerDirectoryContent()
                        {
                            Id = (idVal + 1).ToString(),
                            Name = child.Name,
                            Size = child.Content.Length,
                            DateCreated = DateTime.Now.ToString(),
                            DateModified = DateTime.Now.ToString(),
                            Type = child.Type,
                            HasChild = false,
                            ParentId = idValue.ToString(),
                            IsFile = true,
                            Content = child.Content,
                            isRoot = String.IsNullOrEmpty(this.firebaseGetData.Where(x => x.Id == idVal.ToString()).Select(x => x).ToArray()[0].ParentId) ? true : false,
                            FilterPath = this.GetRelativePath(idValue.ToString(), "/").Substring(this.rootNode.Length) + "/",
                            FilterId = this.GetRelativeId(idValue.ToString()) + "/"
                        };
                        this.updateDBNode(CreateData, idVal);
                    }
                }
                this.filterPath = this.filterId = null;
                foreach (FileManagerDirectoryContent child in childs)
                {
                    if (!child.IsFile)
                    {
                        this.copyFolderItems(child, this.firebaseGetData.Where(x => x.Id == (idValue).ToString()).Select(x => x).ToArray()[0]);
                    }
                }
            }
        }
        //Updates the file system in the firebase database 
        public void updateDBNode(FileManagerDirectoryContent CreateData, int idValue)
        {
            string createdString = JsonConvert.SerializeObject(CreateData, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
            this.firebaseAPI.Patch(this.APIURL + "/" + this.rootNode + "/", "{" + (idValue + 1).ToString() + ":" + createdString + "}");
        }

        // Copies file(s) or folder(s).
        public FileManagerResponse Copy(string path, string targetPath, string[] names, string[] renameFiles, FileManagerDirectoryContent targetData, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse copyResponse = new FileManagerResponse();
            List<string> children = this.firebaseGetData.Where(x => x.ParentId == data[0].Id).Select(x => x.Id).ToList();
            if (children.IndexOf(targetData.Id) != -1 || data[0].Id == targetData.Id)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "400";
                er.Message = "The destination folder is the subfolder of the source folder.";
                copyResponse.Error = er;
                return copyResponse;
            }
            foreach (FileManagerDirectoryContent item in data)
            {
                try
                {
                    int idValue = this.updatedNodeId();
                    if (item.IsFile)
                    {
                        // Copy the file
                        List<FileManagerDirectoryContent> i = this.firebaseGetData.Where(x => x.Id == item.Id).Select(x => x).ToList();
                        FileManagerDirectoryContent CreateData = new FileManagerDirectoryContent()
                        {
                            Id = (idValue + 1).ToString(),
                            Name = item.Name,
                            Size = i[0].Content.Length,
                            DateCreated = DateTime.Now.ToString(),
                            DateModified = DateTime.Now.ToString(),
                            Type = i[0].Type,
                            HasChild = false,
                            ParentId = targetData.Id,
                            IsFile = true,
                            Content = i[0].Content,
                            isRoot = String.IsNullOrEmpty(targetData.ParentId) ? true : false,
                            FilterPath = this.GetRelativePath(targetData.Id, "/").Substring(this.rootNode.Length) + "/",
                            FilterId = this.GetRelativeId(targetData.Id) + "/"

                        };
                        copyFiles.Add(CreateData);
                        this.updateDBNode(CreateData, idValue);
                        this.filterPath = this.filterId = null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
            }
            foreach (FileManagerDirectoryContent item in data)
            {
                try
                {
                    if (!item.IsFile)
                    {
                        this.copyFolderItems(item, targetData);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: " + e.Message);
                    return null;
                }
            }
            copyResponse.Files = copyFiles;
            return copyResponse;
        }

        //Move the files or folders from it current parent item to target item
        public FileManagerResponse Move(string path, string targetPath, string[] names, string[] renameFiles, FileManagerDirectoryContent targetData, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse moveResponse = new FileManagerResponse();
            foreach (FileManagerDirectoryContent i in data)
            {
                List<string> children = this.firebaseGetData.Where(x => x.ParentId == i.Id).Select(x => x.Id).ToList();
                if (children.IndexOf(targetData.Id) != -1 || data[0].Id == targetData.Id)
                {
                    ErrorDetails er = new ErrorDetails();
                    er.Code = "400";
                    er.Message = "The destination folder is the subfolder of the source folder.";
                    moveResponse.Error = er;
                    return moveResponse;
                }
                foreach (FileManagerDirectoryContent item in data)
                {
                    try
                    {
                        // Move the file or folder
                        this.filterPath = this.filterId = null;
                        this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + i.Id, JsonConvert.SerializeObject(new UpdateParentId() { parentId = targetData.Id, isRoot = String.IsNullOrEmpty(targetData.ParentId) ? true : false, filterId = "", filterPath = "" }));
                        this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + i.Id, JsonConvert.SerializeObject(new UpdateParentId() { parentId = targetData.Id, isRoot = String.IsNullOrEmpty(targetData.ParentId) ? true : false, filterId = this.GetRelativeId(targetData.Id), filterPath = this.GetRelativePath(targetData.Id, "/").Substring(this.rootNode.Length) + "/" }));
                        this.UpdateFirebaseJSONData();
                        this.updateChildPath(item);
                        FileManagerDirectoryContent[] targetItem = this.firebaseGetData.Where(x => x.Id == targetData.Id).Select(x => x).ToArray();
                        if (targetItem[0].HasChild == false)
                        {
                            this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + targetData.Id, JsonConvert.SerializeObject(new UpdateChild() { hasChild = true, dateModified = DateTime.Now.ToString() }));
                        }
                        copyFiles.Add(this.firebaseGetData.Where(x => x.Id == item.Id).Select(x => x).ToArray()[0]);
                        if (this.firebaseGetData.Where(x => x.ParentId == item.ParentId && x.Type == "folder").Select(x => x).ToArray().Length < 1)
                        {
                            this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + item.ParentId, JsonConvert.SerializeObject(new UpdateChild() { hasChild = false, dateModified = DateTime.Now.ToString() }));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("An error occurred: " + e.Message);
                        return null;
                    }
                }
            }
            moveResponse.Files = copyFiles;
            return moveResponse;
        }
        public void updateChildPath(FileManagerDirectoryContent item)
        {
            this.filterPath = this.filterId = null;
            FileManagerDirectoryContent[] childs = this.firebaseGetData.Where(x => x.ParentId == item.Id).Select(x => x).ToArray();
            if (childs.Length > 0)
            {
                foreach (FileManagerDirectoryContent child in childs)
                {
                    this.getFirebaseRootNode.Patch(this.APIURL + "/" + this.rootNode + "/" + child.Id, JsonConvert.SerializeObject(new UpdateParentId() { parentId = item.Id, isRoot = String.IsNullOrEmpty(child.ParentId) ? true : false, filterId = this.GetRelativeId(child.Id) + "/", filterPath = this.GetRelativePath(child.Id, "/").Substring(this.rootNode.Length) + "/" }));
                    FileManagerDirectoryContent[] subchilds = this.firebaseGetData.Where(x => x.ParentId == child.Id).Select(x => x).ToArray();
                    if (subchilds.Length > 0)
                    {
                        foreach (FileManagerDirectoryContent i in subchilds)
                        {
                            this.updateChildPath(child);
                        }
                    }
                }
            }
        }
        // Search for particular file(s) or folder(s).
        public FileManagerResponse Search(string path, string searchString, bool showHiddenItems = false, bool caseSensitive = false, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse searchResponse = new FileManagerResponse();
            try
            {
                char[] i = new Char[] { '*' };
                FileManagerDirectoryContent[] s = this.firebaseGetData.Where(x => x.Name.ToLower().Contains(searchString.TrimStart(i).TrimEnd(i).ToLower())).Select(x => x).ToArray();
                searchResponse.Files = s;
                return searchResponse;
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Code = "404";
                er.Message = e.Message.ToString();
                searchResponse.Error = er;
                return searchResponse;
            }
        }
        // Converts the bytes to definite size values
        public String byteConversion(long fileSize)
        {
            try
            {
                string[] index = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
                if (fileSize == 0)
                {
                    return "0 " + index[0];
                }

                long bytes = Math.Abs(fileSize);
                int loc = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, loc), 1);
                return (Math.Sign(fileSize) * num).ToString() + " " + index[loc];
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        //Returns the image
        public FileStreamResult GetImage(string path, string id, bool allowCompress, ImageSize size, params FileManagerDirectoryContent[] data)
        {
            FirebaseResponse res = this.getFirebaseRootNode.Get(this.APIURL + "/" + this.rootNode + "/" + id);
            dynamic obj = Newtonsoft.Json.JsonConvert.DeserializeObject(res.JSONContent);
            FileManagerDirectoryContent[] firebaseGetData = JsonConvert.DeserializeObject<FileManagerDirectoryContent[]>(getResponse.JSONContent);
            firebaseGetData = firebaseGetData.Where(c => c != null).ToArray();
            List<FileManagerDirectoryContent> cont = firebaseGetData.Where(x => x.Id == id).ToList();
            byte[] fileContent = cont[0].Content;
            if (File.Exists(Path.Combine(Path.GetTempPath(), cont[0].Name)))
            {
                File.Delete(Path.Combine(Path.GetTempPath(), cont[0].Name));
            }
            using (Stream file = File.OpenWrite(Path.Combine(Path.GetTempPath(), cont[0].Name)))
            {
                file.Write(fileContent, 0, fileContent.Length);
            }
            try
            {
                FileStream fileStreamInput = new FileStream(Path.Combine(Path.GetTempPath(), cont[0].Name), FileMode.Open, FileAccess.Read);
                fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                return fileStreamResult;
            }
            catch (Exception ex) { throw ex; }
        }

        // Uploads the file(s) to the files system.
        public virtual FileManagerResponse Upload(string path, IList<IFormFile> uploadFiles, string action, params FileManagerDirectoryContent[] data)
        {
            FileManagerResponse uploadResponse = new FileManagerResponse();
            try
            {
                string filename = Path.GetFileName(uploadFiles[0].FileName);
                string contentType = uploadFiles[0].ContentType;
                int idValue = this.firebaseGetData.Select(x => x.Id).ToArray().Select(int.Parse).ToArray().Max();
                using (FileStream fsSource = new FileStream(Path.Combine(Path.GetTempPath(), filename), FileMode.Create))
                {
                    uploadFiles[0].CopyTo(fsSource);
                    fsSource.Close();
                }
                using (FileStream fsSource = new FileStream(Path.Combine(Path.GetTempPath(), filename), FileMode.Open, FileAccess.Read))
                {
                    BinaryReader br = new BinaryReader(fsSource);
                    long numBytes = new FileInfo(Path.Combine(Path.GetTempPath(), filename)).Length;
                    byte[] bytes = br.ReadBytes((int)numBytes);

                    FileManagerDirectoryContent CreateData = new FileManagerDirectoryContent()
                    {
                        Id = (idValue + 1).ToString(),
                        Name = filename,
                        Size = bytes.Length,
                        DateCreated = DateTime.Now.ToString(),
                        DateModified = DateTime.Now.ToString(),
                        Type = System.IO.Path.GetExtension(filename),
                        HasChild = false,
                        ParentId = data[0].Id,
                        IsFile = true,
                        Content = bytes,
                        isRoot = (data[0].Id.ToString() == "0") ? true : false,
                        FilterPath = this.GetRelativePath(data[0].Id, "/").Substring(this.rootNode.Length) + "/",
                        FilterId = this.GetRelativeId(data[0].Id) + "/"
                    };
                    this.updateDBNode(CreateData, idValue);
                }
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                uploadResponse.Error = er;
                return uploadResponse;
            }
            return uploadResponse;
        }
        // Download file(s) or folder(s) from the file system
        public virtual FileStreamResult Download(string path, string[] names, params FileManagerDirectoryContent[] data)
        {

            List<String> files = new List<String> { };
            foreach (FileManagerDirectoryContent item in data)
            {
                IEnumerable<byte[]> fileProperties = firebaseGetData.Where(x => x.Id == item.Id).Select(x => x.Content);
                byte[] fileContent = null;
                if (item.IsFile)
                {
                    fileContent = fileProperties.SelectMany(i => i).ToArray();
                    if (System.IO.File.Exists(Path.Combine(Path.GetTempPath(), item.Name)))
                    {
                        System.IO.File.Delete(Path.Combine(Path.GetTempPath(), item.Name));
                    }
                    using (Stream file = System.IO.File.OpenWrite(Path.Combine(Path.GetTempPath(), item.Name)))
                    {
                        file.Write(fileContent, 0, fileContent.Length);
                    }
                }
                else
                {
                    Directory.CreateDirectory(Path.GetTempPath() + item.Name);
                }
                if (files.IndexOf(item.Name) == -1)
                {
                    files.Add(item.Name);
                }
            }
            if (files.Count == 1 && data[0].IsFile)
            {
                try
                {
                    FileStream fileStreamInput = new FileStream(Path.Combine(Path.GetTempPath(), files[0]), FileMode.Open, FileAccess.Read);
                    fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                    fileStreamResult.FileDownloadName = files[0];
                }
                catch (Exception ex) { throw ex; }
            }
            else
            {
                ZipArchiveEntry zipEntry;
                ZipArchive archive;
                string tempPath = Path.Combine(Path.GetTempPath(), "files.zip");
                using (archive = ZipFile.Open(tempPath, ZipArchiveMode.Update))
                {
                    for (int i = 0; i < files.Count; i++)
                    {
                        if (!data[i].IsFile)
                        {
                            zipEntry = archive.CreateEntry(data[i].Name + "/");
                            DownloadFolderFiles(data[i].Id, data[i].Name, archive, zipEntry);
                        }
                        else
                        {
                            zipEntry = archive.CreateEntryFromFile(Path.GetTempPath() + files[i], files[i], CompressionLevel.Fastest);
                        }

                    }
                    archive.Dispose();
                    FileStream fileStreamInput = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Delete);
                    fileStreamResult = new FileStreamResult(fileStreamInput, "APPLICATION/octet-stream");
                    fileStreamResult.FileDownloadName = "files.zip";
                    if (System.IO.File.Exists(Path.Combine(Path.GetTempPath(), "files.zip")))
                    {
                        System.IO.File.Delete(Path.Combine(Path.GetTempPath(), "files.zip"));
                    }
                }
            }
            return fileStreamResult;
        }

        // Download files within the folder
        public void DownloadFolderFiles(string Id, string Name, ZipArchive archive, ZipArchiveEntry zipEntry)
        {
            DirectoryInfo info = new DirectoryInfo(Path.GetTempPath() + Name);
            FileManagerDirectoryContent[] childFiles = this.firebaseGetData.Where(x => x.ParentId == Id).Select(x => x).ToArray();
            foreach (FileManagerDirectoryContent child in childFiles)
            {
                if (child.IsFile)
                {
                    byte[][] fileData = this.firebaseGetData.Where(x => x.ParentId == Id && x.IsFile == true).Select(x => x.Content).ToArray();
                    byte[] fileContent = fileData.SelectMany(i => i).ToArray();
                    Stream file;
                    using (file = System.IO.File.OpenWrite(Path.Combine(Path.GetTempPath() + Name, child.Name)))
                    {
                        file.Write(fileContent, 0, fileContent.Length);
                        file.Close();
                        zipEntry = archive.CreateEntryFromFile(Path.Combine(Path.GetTempPath() + Name, child.Name), Name + "\\" + child.Name, CompressionLevel.Fastest);
                    }
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(Path.GetTempPath() + Name) + "\\" + child.Name);
                    archive.CreateEntry(Name + "\\" + child.Name + "/", CompressionLevel.Fastest);
                    DownloadFolderFiles(child.Id, Name + "\\" + child.Name, archive, zipEntry);
                }
            }
        }

        // Updates the casing of the value string
        public string ToCamelCase(FileManagerResponse userData)
        {
            return JsonConvert.SerializeObject(userData, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            });
        }
        private class RenameNode
        {
            public string name;
            public string type;
        }

        private class UpdateChild
        {
            public bool hasChild;
            public string dateModified;
        }

        private class UpdateParentId
        {
            public string parentId;
            public bool isRoot;
            public string filterId;
            public string filterPath;
        }
    }
}