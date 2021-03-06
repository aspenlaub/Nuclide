﻿using System.Threading.Tasks;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Nuclide.Interfaces {
    public interface IObsoletePackageFinder {
        Task FindObsoletePackagesAsync(string solutionFolder, IErrorsAndInfos errorsAndInfos);
    }
}
