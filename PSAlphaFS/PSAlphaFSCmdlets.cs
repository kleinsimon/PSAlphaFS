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
            set { recurse = value; }
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

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { set { force = value; } }

        private List<WildcardPattern> includes = new List<WildcardPattern>();
        private List<WildcardPattern> excludes = new List<WildcardPattern>();
        private bool directory = false;
        private bool file = false;
        private bool name = false;
        private bool force = false;
        private bool recurse = false;
        private DirectoryEnumerationOptions dopt;
        private string filter = "*";

        private Alphaleonis.Win32.Security.PrivilegeEnabler priv;

        protected override void BeginProcessing()
        {
            if (_path == null)
            {
                _path = new string[] { @".\" };
            }
            if (force)
                priv = new Alphaleonis.Win32.Security.PrivilegeEnabler(Alphaleonis.Win32.Security.Privilege.Backup);

            dopt = DirectoryEnumerationOptions.BasicSearch | DirectoryEnumerationOptions.ContinueOnException | DirectoryEnumerationOptions.LargeCache;
            if (recurse)
                dopt |= DirectoryEnumerationOptions.Recursive;
            if (directory && !file)
                dopt |= DirectoryEnumerationOptions.Folders;
            else if (file && !directory)
                dopt |= DirectoryEnumerationOptions.Files;
            else
                dopt |= DirectoryEnumerationOptions.FilesAndFolders;

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
                        enumerateFS(Alphaleonis.Win32.Filesystem.Directory.EnumerateFileSystemEntries(p, (filter ?? ""), dopt));
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
                string filename = Path.GetFileName(f);
                if (includes.Count > 0 && !checkMatchesAny(includes, filename)) continue;
                if (excludes.Count > 0 && checkMatchesAny(excludes, filename)) continue;
                if (name)
                    WriteObject(f);
                else
                {
                    var fi = new FileInfo(f);
                    if (fi.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                        WriteObject(new DirectoryInfo(f));
                    else
                        WriteObject(fi);
                }
            }
        }

        private bool checkMatchesAny(IEnumerable<WildcardPattern> patterns, string compare)
        {
            foreach (var p in patterns)
            {
                if (p.IsMatch(compare.ToLower())) return true;
                if (string.Equals(p.ToString(), compare, StringComparison.OrdinalIgnoreCase)) return true;
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

        [Cmdlet(VerbsCommon.Watch, "Pipeline")]
        public class WatchPipeline : PSCmdlet
        {
            [Parameter(Mandatory = true, ValueFromPipeline = true, HelpMessage = "Objects to measure. By default, these Objects are passed through the pipeline.")]
            public PSObject[] InputObject { get; set; }
            [Parameter(HelpMessage = "The Properties of the input objects to measure (e.g. Length for Files)")]
            public string[] Property { get; set; }
            [Parameter(HelpMessage = "Formats the output Values as Bytes (KB, MB etc)")]
            public SwitchParameter Bytes { get; set; }
            [Parameter(HelpMessage = "Returns an objects with the stats when finished instead of passing through the input objects")]
            public SwitchParameter GetStats { get; set; }
            [Parameter(HelpMessage = "Surpresses live output... if you really want")]
            public SwitchParameter NoOutput { get; set; }

            private Dictionary<string, Data> data = new Dictionary<string, Data>();
            private long count = 0;
            private bool hasData = false;
            public class Data
            {
                public string Name = "";
                public double Sum = 0;
                public double Average = double.NaN;
                public double Maximum = double.NaN;
                public double Minimum = double.NaN;
                public long NotEvaluated = 0;

                public Data(string name)
                {
                    Name = name;
                }
            }
            public class ByteData
            {
                public string Name;
                public ReadableByte Sum;
                public ReadableByte Average;
                public ReadableByte Maximum;
                public ReadableByte Minimum;
                public long NotEvaluated;

                public ByteData(Data src)
                {
                    Name = src.Name;
                    Sum = src.Sum;
                    Average = src.Average;
                    Maximum = src.Maximum;
                    Minimum = src.Minimum;
                    NotEvaluated = src.NotEvaluated;
                }
            }

            ProgressRecord pr = new ProgressRecord(0, "Measuring Objects in Pipeline", "test");

            protected override void BeginProcessing()
            {
                foreach (string parm in Property)
                    data.Add(parm, new Data(parm));
            }

            protected override void ProcessRecord()
            {
                foreach (PSObject o in InputObject)
                {
                    count++;
                    foreach (string parm in Property)
                    {
                        try
                        {
                            double v = Convert.ToDouble(o.Properties[parm].Value);
                            Data d = data[parm];
                            d.Sum += v;
                            d.Average = d.Sum / count;
                            d.Maximum = (double.IsNaN(d.Maximum)) ? v : Math.Max(d.Maximum, v);
                            d.Minimum = (double.IsNaN(d.Minimum)) ? v : Math.Min(d.Minimum, v);
                            hasData = true;
                            if (!NoOutput)
                                DisplayRes();
                        }
                        catch
                        {
                            data[parm].NotEvaluated++;
                        }
                    }
                    if (!GetStats)
                        WriteObject(o);
                }
            }

            protected override void EndProcessing()
            {
                if (GetStats)
                {
                    if (hasData)
                        if (Bytes)
                            foreach (var d in data.Values)
                                WriteObject(new ByteData(d));
                        else
                            WriteObject(data.Values.ToArray());
                    else
                        WriteObject(count);
                }
            }

            protected override void StopProcessing()
            {
                WriteWarning("Stopped Operation");
                EndProcessing();
            }

            private void DisplayRes()
            {
                string prog = string.Format("Count={0,4}", count);
                foreach (var kv in data)
                {
                    var parm = kv.Key;
                    dynamic dat;

                    if (Bytes)
                        dat = new ByteData(kv.Value);
                    else
                        dat = kv.Value;

                    prog += string.Format("{0,10}: {1,4}={2,8}{3,8}={4,8}{5,8}={6,8}{7,8}={8,8}", parm, "Sum", dat.Sum, "Average", dat.Average, "Max", dat.Maximum, "Min", dat.Minimum);
                }
                pr.CurrentOperation = prog;
                CommandRuntime.WriteProgress(pr);
            }
        }
        public class ReadableByte
        {
            public double Value { get; private set; }

            public ReadableByte(double d)
            {
                Value = d;
            }

            public static implicit operator ReadableByte(double d)
            {
                return new ReadableByte(d);
            }

            public static implicit operator double(ReadableByte r)
            {
                return r.Value;
            }

            public override string ToString()
            {
                return B2S(this);
            }

            string B2S(double byteCount)
            {
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
                if (byteCount == 0)
                    return "0" + suf[0];
                double bytes = Math.Abs(byteCount);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return (Math.Sign(byteCount) * num).ToString() + " " + suf[place];
            }
        }
    }
}
