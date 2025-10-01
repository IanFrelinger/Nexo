using NUnit.Framework;
using UnityEngine.TestTools;
using System.Collections;

public class SimplePlaymodeSmoke
{
    [UnityTest]
    public IEnumerator passes_in_one_frame()
    {
        yield return null;
        Assert.Pass();
    }
}
