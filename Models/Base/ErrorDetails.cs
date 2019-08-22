using System;
using System.Collections.Generic;
using System.Linq;


namespace Syncfusion.EJ2.FileManager.Base
{
    public class ErrorDetails
    {
        public string Code { get; set; }

        public string Message { get; set; }

        public IEnumerable<string> FileExists { get; set; }
    }
}