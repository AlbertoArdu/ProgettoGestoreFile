using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clientWPF
{

    public class FileVersion
    {
        private string fileName;
        private DateTime timestamp;
        private Int64 versionNum;

        public FileVersion(string fileName, Int64 version, DateTime timestamp)
        {
            this.fileName = fileName;
            this.versionNum = version;
            this.timestamp = timestamp;
        }

        public string FileName
        {
            get { return this.fileName; }
        }

        public Int64 VersionNum
        {
            get { return this.versionNum; }
        }

        public DateTime Timestamp
        {
            get { return this.timestamp; }
        }

    }
}