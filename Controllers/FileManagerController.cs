using Syncfusion.EJ2.FileManager.PhysicalFileProvider;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Syncfusion.EJ2.FileManager.Base;
using System.IO;
using Microsoft.Net.Http.Headers;
using System.Reflection;

namespace EJ2APIServices.Controllers
{

    [Route("api/[controller]")]
    [EnableCors("AllowAllOrigins")]
    public class FileManagerController : Controller
    {
        public PhysicalFileProvider operation;
        public string basePath;
        string root = "wwwroot\\Files";
        private readonly IWebHostEnvironment _hostingEnvironment;
        FileManagerResponse readResponse = new FileManagerResponse();
        public FileManagerController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
            this.basePath = hostingEnvironment.ContentRootPath;
            this.operation = new PhysicalFileProvider();
            this.operation.RootFolder(this.basePath + "\\" + this.root);
        }
        [Route("FileOperations")]
        public object FileOperations([FromBody] FileManagerDirectoryContent args)
        {
            try
            {
                string ValidatePath = this.basePath + "\\" + this.root + args.Path;
                if (Path.GetFullPath(ValidatePath) != (Path.GetDirectoryName(ValidatePath) + Path.DirectorySeparatorChar))
                {
                    throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                }
                if(args.Names !=null && args.Names.Length> 0)
                {
                    for (int i = 0; i < args.Names.Length; i++)
                    {
                        string ValidateName = Path.Combine((this.basePath + "\\" + this.root + args.Path), (args.Names.Length > 0 ? args.Names[i] : ""));
                        if (Path.GetFullPath(ValidateName) != (Path.GetDirectoryName(ValidateName) + Path.DirectorySeparatorChar + args.Names[i]))
                        {
                            throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                        }
                    }
                }
                if(args.NewName!=null)
                {
                    string ValidateNewName = Path.Combine((this.basePath + "\\" + this.root + args.Path), (args.NewName!=null ? args.NewName : ""));
                    if (Path.GetFullPath(ValidateNewName) != (Path.GetDirectoryName(ValidateNewName) + Path.DirectorySeparatorChar+ args.NewName))
                    {
                        throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                    }
                }
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = "417";
                er.Message = "Access denied for Directory-traversal"; 
                readResponse.Error = er;
                return readResponse;
            }
            if (args.Action == "delete" || args.Action == "rename")
            {
                if ((args.TargetPath == null) && (args.Path == ""))
                {
                    FileManagerResponse response = new FileManagerResponse();
                    response.Error = new ErrorDetails { Code = "401", Message = "Restricted to modify the root folder." };
                    return this.operation.ToCamelCase(response);
                }
            }
            switch (args.Action)
            {
                case "read":
                    // reads the file(s) or folder(s) from the given path.
                    return this.operation.ToCamelCase(this.operation.GetFiles(args.Path, args.ShowHiddenItems));

                case "delete":
                    // deletes the selected file(s) or folder(s) from the given path.
                    this.operation.Response = Response;
                    return this.operation.ToCamelCase(this.operation.Delete(args.Path, args.Names));
                case "copy":
                    // copies the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                    return this.operation.ToCamelCase(this.operation.Copy(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                case "move":
                    // cuts the selected file(s) or folder(s) from a path and then pastes them into a given target path.
                    return this.operation.ToCamelCase(this.operation.Move(args.Path, args.TargetPath, args.Names, args.RenameFiles, args.TargetData));
                case "details":
                    // gets the details of the selected file(s) or folder(s).
                    return this.operation.ToCamelCase(this.operation.Details(args.Path, args.Names, args.Data));
                case "create":
                    // creates a new folder in a given path.
                    return this.operation.ToCamelCase(this.operation.Create(args.Path, args.Name));
                case "search":
                    // gets the list of file(s) or folder(s) from a given path based on the searched key string.
                    return this.operation.ToCamelCase(this.operation.Search(args.Path, args.SearchString, args.ShowHiddenItems, args.CaseSensitive));
                case "rename":
                    // renames a file or folder.
                    return this.operation.ToCamelCase(this.operation.Rename(args.Path, args.Name, args.NewName, false, args.ShowFileExtension, args.Data));
            }
            return null;
        }

        // uploads the file(s) into a specified path
        [Route("Upload")]
        public IActionResult Upload(string path, IList<IFormFile> uploadFiles, string action)
        {
            try
            {
                string ValidatePath = this.basePath + "\\" + this.root + path;
                if (Path.GetFullPath(ValidatePath) != (Path.GetDirectoryName(ValidatePath) + Path.DirectorySeparatorChar))
                {
                    throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                }
                foreach (IFormFile file in uploadFiles)
                {
                    if (uploadFiles != null)
                    {
                        var name = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim().ToString();
                        string[] folders = name.Split('/');
                        string fileName = folders[folders.Length - 1];
                        var fullName = Path.Combine((this.basePath + "\\" + this.root + path), fileName);
                        if (Path.GetFullPath(fullName) != (Path.GetDirectoryName(fullName) + Path.DirectorySeparatorChar) + fileName)
                        {
                            throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = "417";
                er.Message = "Access denied for Directory-traversal";
                readResponse.Error = er;
                return Content("");
            }
            FileManagerResponse uploadResponse;
            foreach (var file in uploadFiles)
            {
                var folders = (file.FileName).Split('/');
                // checking the folder upload
                if (folders.Length > 1)
                {
                    for (var i = 0; i < folders.Length - 1; i++)
                    {
                        string newDirectoryPath = Path.Combine(this.basePath + path, folders[i]);
                        if (!Directory.Exists(newDirectoryPath))
                        {
                            this.operation.ToCamelCase(this.operation.Create(path, folders[i]));
                        }
                        path += folders[i] + "/";
                    }
                }
            }
            uploadResponse = operation.Upload(path, uploadFiles, action, null);
            if (uploadResponse.Error != null)
            {
               Response.Clear();
               Response.ContentType = "application/json; charset=utf-8";
               Response.StatusCode = Convert.ToInt32(uploadResponse.Error.Code);
               Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = uploadResponse.Error.Message;
            }
            return Content("");
        }

        // downloads the selected file(s) and folder(s)
        [Route("Download")]
        public IActionResult Download(string downloadInput)
        {
            FileManagerDirectoryContent args = JsonConvert.DeserializeObject<FileManagerDirectoryContent>(downloadInput);
            try
            {
                string ValidatePath = this.basePath + "\\" + this.root + args.Path;
                if (Path.GetFullPath(ValidatePath) != (Path.GetDirectoryName(ValidatePath) + Path.DirectorySeparatorChar))
                {
                    throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                }
                if(args.Names!=null && args.Names.Length > 0) 
                { 
                    for (int i = 0; i < args.Names.Length; i++)
                    {
                        string fullPath = Path.Combine((this.basePath + "\\" + this.root + args.Path), args.Names.Length > 0 ? args.Names[i] : "");
                        if (Path.GetFullPath(fullPath) != (Path.GetDirectoryName(fullPath) + Path.DirectorySeparatorChar + args.Names[i]))
                        {
                            throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = "417";
                er.Message = "Access denied for Directory-traversal";
                readResponse.Error = er;
                return Content("");
            }
            return operation.Download(args.Path, args.Names, args.Data);
        }

        // gets the image(s) from the given path
        [Route("GetImage")]
        public IActionResult GetImage(FileManagerDirectoryContent args)
        {
            try 
            { 
                String fullPath = (this.basePath + "\\" + this.root + args.Path);
                if (Path.GetFullPath(fullPath) != (Path.GetDirectoryName(fullPath) + Path.DirectorySeparatorChar)+ (fullPath.Substring(fullPath.LastIndexOf("/") + 1)))
                {
                    throw new UnauthorizedAccessException("Access denied for Directory-traversal");
                }
            }
            catch (Exception e)
            {
                ErrorDetails er = new ErrorDetails();
                er.Message = e.Message.ToString();
                er.Code = "417";
                er.Message = "Access denied for Directory-traversal";
                readResponse.Error = er;
                return Content("");
            }
            return this.operation.GetImage(args.Path, args.Id,false,null, null);
        }       
    }

}
