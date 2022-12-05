using System.Management.Automation;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.ObjectModel;

string defaultHeader = "Content-Type: text/html" + Environment.NewLine + Environment.NewLine;
string psargs = "";
string psargsErr = "";

try
{
    psargs = Environment.GetEnvironmentVariable("QUERY_STRING");
    psargs = Uri.UnescapeDataString(psargs);
    psargs = psargs.Replace("\"", "\"\"");
    string[] psargarray = psargs.Split('&');
    for (int i = 0; i < psargarray.Length; i++)
    {
        psargarray[i] = Regex.Replace(psargarray[i], @"(\w+)=(.*)", "-$1 \"$2\"");
    }
    psargs = String.Join(" ", psargarray);
}
catch (Exception ex)
{
    psargsErr = ex.Message;
}

if (Environment.GetEnvironmentVariable("REQUEST_METHOD") == "POST") //experimental POST data
{
    System.IO.Stream s = Console.OpenStandardInput();
    System.IO.BinaryReader br = new System.IO.BinaryReader(s);
    string Length = Environment.GetEnvironmentVariable("CONTENT_LENGTH");
    int Size = Int32.Parse(Length);
    byte[] Data = new byte[Size];
    br.Read(Data, 0, Size);
    // *** don’t close the reader!
    Environment.SetEnvironmentVariable("POST_DATA", System.Text.Encoding.Default.GetString(Data, 0, Size));
}

/// EXECUTE SCRIPT SECTION

string OutputBuffer = "";

using (PowerShell PowerShellInst = PowerShell.Create())
{
    PowerShellInst.AddCommand("Set-ExecutionPolicy").AddArgument("Unrestricted")
    .AddParameter("Scope", "CurrentUser");
    PowerShellInst.AddScript(args[0], true);
    try
    {
        Collection<PSObject> results = PowerShellInst.Invoke();
        // close the runspace

        StringBuilder stringBuilder = new StringBuilder();
        foreach (PSObject obj in results)
        {
            stringBuilder.AppendLine(obj.ToString());
        }

        if (PowerShellInst.HadErrors)
        {
            foreach(var error in PowerShellInst.Streams.Error)
            {
                stringBuilder.AppendLine(error.ToString());
            }
        }

        OutputBuffer = stringBuilder.ToString();
    }
    catch (Exception ex)
    {
        //string template = @""
        OutputBuffer = ex.Message;
    }
}

/// END EXEC SCRIPT SECTION

OutputBuffer = OutputBuffer.Trim('\r', '\n');
//
if (!(Regex.Match(OutputBuffer, "[C,c]ontent-[T,t]ype:.*").Success))
{
    OutputBuffer = defaultHeader + OutputBuffer;
}

Console.Write(OutputBuffer);