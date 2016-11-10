using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Alphaleonis.Win32.Filesystem;

namespace PSAlphaFS
{
    [Cmdlet(VerbsCommon.Get, "LongChildItem", DefaultParameterSetName = "Path")]
    public class GetLongChildItem : PSCmdlet
    {
        [Alias("Path")]
        [Parameter(
            Position = 0,
            Mandatory = false,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Path")
            ]
        public string[] FullName
        {
            set
            {
                _path = value;
            }
        }
        [Parameter(
            Position = 0,
            Mandatory = false,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Literal")
        ]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        public string[] LiteralPath
        {
            set
            {
                _litpath = value;
            }
        }
        private string[] _path, _litpath;

        // Filter wildcard string 
        [Parameter(Mandatory = false, Position = 1)]
        public string Filter { set { filter = value; } }

        // Enumerate Subdirectories
        [Parameter(Mandatory = false)]
        public SwitchParameter Recurse
        {
            set
            {
                search_option = (value) ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly;
            }
        }

        // Multiple string names to exclude    
        [Parameter(Mandatory = false)]
        public string[] Include
        {
            set
            {
                foreach (string v in value)
                {
                    includes.Add(new WildcardPattern(v.ToLower()));
                }
            }
        }

        // Multiple string names to include    
        [Parameter(Mandatory = false)]
        public string[] Exclude
        {
            set
            {
                foreach (string v in value)
                {
                    excludes.Add(new WildcardPattern(v.ToLower()));
                }
            }
        }

        // Get Only Folders
        [Parameter(Mandatory = false)]
        public SwitchParameter Directory { set { directory = value; } }

        // Get Only Files
        [Parameter(Mandatory = false)]
        public SwitchParameter File { set { file = value; } }

        // Get Only File or Folder Names
        [Parameter(Mandatory = false)]
        public SwitchParameter Name { set { name = value; } }

        private System.IO.SearchOption search_option;
        private List<WildcardPattern> includes = new List<WildcardPattern>();
        private List<WildcardPattern> excludes = new List<WildcardPattern>();
        private bool directory = false;
        private bool file = false;
        private bool name = false;
        private string filter = "*";

        private Alphaleonis.Win32.Security.PrivilegeEnabler priv;
        private List<string> Paths = new List<string>();

        protected override void BeginProcessing()
        {
            if (_path == null)
            {
                _path = new string[] { @".\" };
            }

            ProviderInfo provider;
            PSDriveInfo drive;

            if (_path != null)
                foreach (var s in _path)
                    Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

            if (_litpath != null)
                foreach (var s in _litpath)
                    Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));
        }

        protected override void ProcessRecord()
        {
            try
            {
                foreach (string p in Paths)
                {
                    FileInfo pO = new FileInfo(p);
                    if (pO.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                    {
                        if (directory && !file) enumerateDirs(p);
                        else if (!directory && file) enumerateFiles(p);
                        else enumerateAll(p);
                    }
                    else
                    {
                        if (name) WriteObject(p);
                        else WriteObject(new FileInfo(p));
                    }
                }
            }
            catch (PipelineStoppedException e)
            {
                return;
            }
        }

        protected override void EndProcessing()
        {
            if (priv != null) priv.Dispose();
            base.EndProcessing();
        }

        protected override void StopProcessing()
        {
            if (priv != null) priv.Dispose();
            base.StopProcessing();
        }


        private void enumerateDirs(string path)
        {
            IEnumerable<string> dirs = Alphaleonis.Win32.Filesystem.Directory.EnumerateDirectories(path, (filter ?? "*"), search_option);
            foreach (string dir in dirs)
            {
                string dirname = Alphaleonis.Win32.Filesystem.Path.GetFileName(dir);
                if (includes.Count > 0 && !checkMatchesAny(includes, dirname)) continue;
                if (checkMatchesAny(excludes, dirname)) continue;
                if (name)
                    WriteObject(dir);
                else
                    WriteObject(new DirectoryInfo(dir));
            }
        }

        private void enumerateFiles(string path)
        {
            IEnumerable<string> files = Alphaleonis.Win32.Filesystem.Directory.EnumerateFiles(path, (filter ?? "*"), search_option);
            foreach (string f in files)
            {
                string filename = Alphaleonis.Win32.Filesystem.Path.GetFileName(f);
                if (includes.Count > 0 && !checkMatchesAny(includes, filename)) continue;
                if (checkMatchesAny(excludes, filename)) continue;
                if (name)
                    WriteObject(f);
                else
                    WriteObject(new FileInfo(f));
            }
        }

        private void enumerateAll(string path)
        {
            IEnumerable<string> dirs = Alphaleonis.Win32.Filesystem.Directory.EnumerateFileSystemEntries(path, (filter ?? "*"), search_option);
            foreach (string dir in dirs)
            {
                string dirname = Alphaleonis.Win32.Filesystem.Path.GetFileName(dir);
                if (includes.Count > 0 && !checkMatchesAny(includes, dirname)) continue;
                if (checkMatchesAny(excludes, dirname)) continue;
                if (name)
                    WriteObject(dir);
                else
                    WriteObject(new FileInfo(dir));
            }
        }

        private bool checkMatchesAny(IEnumerable<WildcardPattern> patterns, string compare)
        {
            foreach (var p in patterns)
            {
                if (p.IsMatch(compare.ToLower())) return true;
            }
            return false;
        }
    }

    [Cmdlet(VerbsCommon.Get, "LongItem", DefaultParameterSetName = "Path")]
    public class GetLongItem : PSCmdlet
    {
        [Alias("Path")]
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Path")
            ]
        public string[] FullName
        {
            set
            {
                _path = value;
            }
        }
        [Parameter(
            Position = 0,
            Mandatory = false,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Literal")
        ]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        public string[] LiteralPath
        {
            set
            {
                _litpath = value;
            }
        }
        private string[] _path, _litpath;
        private List<string> Paths = new List<string>();

        protected override void BeginProcessing()
        {
            if (_path == null)
            {
                _path = new string[] { this.SessionState.Path.CurrentFileSystemLocation.Path };
            }
            ProviderInfo provider;
            PSDriveInfo drive;

            if (_path != null)
                foreach (var s in _path)
                    Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

            if (_litpath != null)
                foreach (var s in _litpath)
                    Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));
        }

        protected override void ProcessRecord()
        {

            foreach (var ptmp in Paths)
            {
                var p = this.GetUnresolvedProviderPathFromPSPath(ptmp);
                WriteObject(new FileInfo(p));
            }
        }
    }

    [Cmdlet(VerbsCommon.Copy, "LongItem", DefaultParameterSetName = "Path")]
    public class CopyLongItem : PSCmdlet
    {
        [Alias("Path")]
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Path")
            ]
        public string[] FullName
        {
            set
            {
                _path = value;
            }
        }
        [Parameter(
            Position = 0,
            Mandatory = false,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Literal")
        ]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        public string[] LiteralPath
        {
            set
            {
                _litpath = value;
            }
        }
        private string[] _path, _litpath;
        public string Destination { get; set; }
        [Parameter()]
        public SwitchParameter Force { get; set; }

        private List<string> Paths = new List<string>();

        protected override void BeginProcessing()
        {
            ProviderInfo provider;
            PSDriveInfo drive;

            if (_path != null)
                foreach (var s in _path)
                    Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

            if (_litpath != null)
                foreach (var s in _litpath)
                    Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));
        }

        protected override void ProcessRecord()
        {
            string dstpath = this.GetUnresolvedProviderPathFromPSPath(Destination);
            FileInfo dstobj = new FileInfo(dstpath);

            foreach (string origpath in Paths)
            {
                string tmpdst = dstpath;
                FileInfo pO = new FileInfo(origpath);
                if (Directory.Exists(dstpath))
                {
                    tmpdst = Path.Combine(dstpath, Path.GetFileName(origpath));
                }

                if (pO.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                    Directory.Copy(origpath, tmpdst, Force);
                else
                    File.Copy(origpath, tmpdst, Force);
            }
        }
    }

    [Cmdlet(VerbsCommon.Remove, "LongItem", DefaultParameterSetName = "Path")]
    public class RemoveLongItem : PSCmdlet
    {
        [Alias("Path")]
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Path")
            ]
        public string[] FullName
        {
            set
            {
                _path = value;
            }
        }
        [Parameter(
            Position = 0,
            Mandatory = false,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Literal")
        ]
        [Alias("PSPath")]
        [ValidateNotNullOrEmpty]
        public string[] LiteralPath
        {
            set
            {
                _litpath = value;
            }
        }
        private string[] _path, _litpath;

        [Parameter()]
        public SwitchParameter Recurse { get; set; }
        [Parameter()]
        public SwitchParameter Force { get; set; }

        private List<string> Paths = new List<string>();

        protected override void BeginProcessing()
        {
            ProviderInfo provider;
            PSDriveInfo drive;

            if (_path != null)
                foreach (var s in _path)
                    Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

            if (_litpath != null)
                foreach (var s in _litpath)
                    Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));
        }

        protected override void ProcessRecord()
        {
            foreach (string origpath in Paths)
            {
                FileInfo pO = new FileInfo(origpath);

                if (pO.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                    Directory.Delete(origpath, Recurse, Force);
                else
                    File.Delete(origpath, Force);
            }
        }
    }
}
