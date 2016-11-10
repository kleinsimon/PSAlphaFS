using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Alphaleonis.Win32.Filesystem;

namespace PSAlphaFSnet
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

        protected override void BeginProcessing()
        {
            if (_path == null)
            {
                _path = new string[] { @".\" };
            }

            priv = new Alphaleonis.Win32.Security.PrivilegeEnabler(Alphaleonis.Win32.Security.Privilege.Backup);
        }

        protected override void ProcessRecord()
        {
            try
            {
                ProviderInfo provider;
                PSDriveInfo drive;
                List<string> Paths = new List<string>();
                if (_path != null)
                    foreach (var s in _path)
                        Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

                if (_litpath != null)
                    foreach (var s in _litpath)
                        Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));

                foreach (string p in Paths)
                {
                    FileInfo pO = new FileInfo(p);
                    if (pO.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                    {
                        if (directory && !file) enumerateFS(Alphaleonis.Win32.Filesystem.Directory.EnumerateDirectories(p, (filter ?? "*"), search_option));
                        else if (!directory && file) enumerateFS(Alphaleonis.Win32.Filesystem.Directory.EnumerateFiles(p, (filter ?? "*"), search_option));
                        else enumerateFS(Alphaleonis.Win32.Filesystem.Directory.EnumerateFileSystemEntries(p, (filter ?? "*"), search_option));
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

        private void enumerateFS(IEnumerable<string> path)
        {

            foreach (string f in path)
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

        protected override void BeginProcessing()
        {
            if (_path == null)
            {
                _path = new string[] { this.SessionState.Path.CurrentFileSystemLocation.Path };
            }
        }

        protected override void ProcessRecord()
        {
            ProviderInfo provider;
            PSDriveInfo drive;
            List<string> Paths = new List<string>();
            if (_path != null)
                foreach (var s in _path)
                    Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

            if (_litpath != null)
                foreach (var s in _litpath)
                    Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));

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
        [Parameter(
            Position = 1,
            Mandatory = false,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true)
        ]
        public string Destination { get; set; }
        [Parameter()]
        public SwitchParameter Force { get; set; }

        protected override void BeginProcessing()
        {
            
        }

        protected override void ProcessRecord()
        {
            ProviderInfo provider;
            PSDriveInfo drive;
            List<string> Paths = new List<string>();
            if (_path != null)
                foreach (var s in _path)
                    Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

            if (_litpath != null)
                foreach (var s in _litpath)
                    Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));

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

    [Cmdlet(VerbsCommon.Move, "LongItem", DefaultParameterSetName = "Path")]
    public class MoveLongItem : PSCmdlet
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
        [Parameter(
            Position = 1,
            Mandatory = true,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = true)
        ]
        public string Destination { get; set; }
        [Parameter()]
        public SwitchParameter Force { get; set; }

        protected override void BeginProcessing()
        {
            
        }

        protected override void ProcessRecord()
        {
            ProviderInfo provider;
            PSDriveInfo drive;
            List<string> Paths = new List<string>();

            if (_path != null)
                foreach (var s in _path)
                    Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

            if (_litpath != null)
                foreach (var s in _litpath)
                    Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));

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
                    Directory.Move(origpath, tmpdst, (Force) ? MoveOptions.ReplaceExisting : MoveOptions.None);
                else
                    File.Move(origpath, tmpdst, (Force) ? MoveOptions.ReplaceExisting : MoveOptions.None);
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

        protected override void BeginProcessing()
        {
            
        }

        protected override void ProcessRecord()
        {
            ProviderInfo provider;
            PSDriveInfo drive;
            List<string> Paths = new List<string>();

            if (_path != null)
                foreach (var s in _path)
                    Paths.AddRange(this.GetResolvedProviderPathFromPSPath(s, out provider));

            if (_litpath != null)
                foreach (var s in _litpath)
                    Paths.Add(this.SessionState.Path.GetUnresolvedProviderPathFromPSPath(s, out provider, out drive));

            foreach (string origpath in Paths)
            {
                FileInfo pO = new FileInfo(origpath);

                if (pO.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                    Directory.Delete(origpath, Recurse, Force);
                else
                    File.Delete(origpath, Force);
            }
        }

        [Cmdlet("Monitor", "Collection")]
        public class MonitorCollection : Cmdlet
        {
            [Parameter(Mandatory = true, ValueFromPipeline = true)]
            public PSObject[] InputObject { get; set; }
            [Parameter()]
            public string[] Property { get; set; }

            private Dictionary<string, data> Data = new Dictionary<string, data>();
            private int Count = 0;
            class data
            {
                public double Sum = 0;
                public double Average = double.NaN;
                public double Maximum = double.NaN;
                public double Minimum = double.NaN;
            }

            protected override void BeginProcessing()
            {
                foreach (string parm in Property)
                    Data.Add(parm, new data());

            }

            protected override void ProcessRecord()
            {
                foreach (PSObject o in InputObject)
                {
                    Count++;
                    foreach (string parm in Property)
                    {
                        try
                        {
                            double v = (double)o.Properties[parm].Value;
                            Data[parm].Sum += v;
                            Data[parm].Average = Data[parm].Sum / (double)Count;
                            Data[parm].Maximum = (Data[parm].Maximum == double.NaN) ? v : Math.Max(Data[parm].Maximum, v);
                            Data[parm].Minimum = (Data[parm].Minimum == double.NaN) ? v : Math.Min(Data[parm].Minimum, v);
                        }
                        catch
                        {

                        }
                    }
                    DisplayRes();
                }
            }

            protected override void EndProcessing()
            {
                WriteObject(Data);
            }

            private void DisplayRes()
            {
                string prog = string.Format("`rCount:`t%n", Count);
                foreach (var kv in Data)
                {
                    var parm = kv.Key;
                    var data = kv.Value;

                    prog += string.Format("%s: %s=%f`t%s=%f`t%s=%f`t%s=%f`t", parm, "Sum", data.Sum, "Average", data.Average, "Max", data.Maximum, "Min", data.Minimum);
                }
                WriteVerbose(prog);
            }
        }
    }
}
