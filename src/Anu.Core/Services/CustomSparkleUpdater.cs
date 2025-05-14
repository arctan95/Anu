using NetSparkleUpdater.Interfaces;

namespace Anu.Core.Services;

public class CustomSparkleUpdater : NetSparkleUpdater.SparkleUpdater
{
    public CustomSparkleUpdater(string appcastUrl, ISignatureVerifier signatureVerifier)
            : base(appcastUrl, signatureVerifier, null)
        { }
}