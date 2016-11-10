using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using Alphaleonis.Win32.Filesystem;

namespace PSAlphaFS
{
    [Cmdlet(VerbsCommon.Get, "LongChildItem", DefaultParameterSetName = "All")]
    public class GetLongChildItem : PSCmdlet
    {
        [Parameter(Mandatory = false,
                ValueFromPipelineByPropertyName = true,
                ValueFromPipeline = true,
        Position = 0)]
        public string[] Path { set { path = value; } }

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
                    includes.Add(new WildcardPattern(v));
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
                    excludes.Add(new WildcardPattern(v));
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
        private string[] path = null;

        private bool dodirs = false;
        private bool dofiles = false;

        private Alphaleonis.Win32.Security.PrivilegeEnabler priv;

        protected override void BeginProcessing()
        {
            dofiles = file || (!file && !directory);
            dodirs = directory || (!file && !directory);
            if (path == null)
            {
                path = new string[] { this.SessionState.Path.CurrentFileSystemLocation.Path };
            }
            priv = new Alphaleonis.Win32.Security.PrivilegeEnabler(Alphaleonis.Win32.Security.Privilege.Backup);
        }

        protected override void ProcessRecord()
        {
            try
            {
                foreach (string p in path)
                {
                    FileInfo pO = new FileInfo(p);
                    if (pO.Attributes.HasFlag(System.IO.FileAttributes.Directory))
                    {
                        if (dodirs) enumerateDirs(p);
                        if (dofiles) enumerateFiles(p);
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
                string dirname = Alphaleonis.Win32.Filesystem.Path.GetDirectoryName(dir);
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

        private bool checkMatchesAny(IEnumerable<WildcardPattern> patterns, string compare)
        {
            foreach (var p in patterns)
            {
                if (p.IsMatch(compare)) return true;
            }
            return false;
        }
    }
}
