using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Duplicati.Library.DynamicLoader;
using Duplicati.Library.Utility;
using Duplicati.Library.Interface;


namespace GitBin.Remotes
{
    public class DuplicatiRemote: IRemote
    {
        private readonly IConfigurationProvider _configurationProvider;
        private readonly IBackend _backend;

        public DuplicatiRemote(IConfigurationProvider configurationProvider)
        {
            this._configurationProvider = configurationProvider;

            string url = this._configurationProvider.DuplicatiUrl;
            string options = this._configurationProvider.DuplicatiOptions;
            _backend = BackendLoader.GetBackend(url, Duplicati.CommandLine.CommandLineParser.ExtractOptions(new List<string>(options.Split(' '))));
        }

        public GitBinFileInfo[] ListFiles()
        {
            return _backend.List().Select(x => new GitBinFileInfo(x.Name, x.Size)).ToArray();
        }

        public void UploadFile(string fullPath, string key)
        {
            if (_backend is IStreamingBackend)
            {
                using (var bs = System.IO.File.OpenRead(fullPath))
                using (var pgs = new ProgressReportingStream(bs, bs.Length))
                {
                    pgs.Progress += (x) =>
                    {
                        if (ProgressChanged != null)
                            ProgressChanged(x);
                    };

                    ((IStreamingBackend)_backend).Put(key, pgs);
                }
            }
            else
            {
                if (ProgressChanged != null)
                    ProgressChanged(0);
                _backend.Put(key, fullPath);
                if (ProgressChanged != null)
                    ProgressChanged(100);
            }
        }

        public void DownloadFile(string fullPath, string key)
        {
            if (_backend is IStreamingBackend)
            {
                using (var bs = System.IO.File.OpenWrite(fullPath))
                using (var pgs = new ProgressReportingStream(bs, bs.Length))
                {
                    pgs.Progress += (x) =>
                    {
                        if (ProgressChanged != null)
                            ProgressChanged(x);
                    };

                    ((IStreamingBackend)_backend).Get(key, pgs);
                }
            }
            else
            {
                if (ProgressChanged != null)
                    ProgressChanged(0);
                _backend.Get(key, fullPath);
                if (ProgressChanged != null)
                    ProgressChanged(100);
            }
        }

        public event Action<int> ProgressChanged;
    }
}
