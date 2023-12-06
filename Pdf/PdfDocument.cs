namespace Pdf;

public class PdfDocument {
    readonly FileStream output;

    readonly List<long> objectOffsets = new();

    readonly List<int> pageObjects = new();

    readonly Dictionary<string, int> fontObjects = new();

    int rootObject;
    int infoObject;
    int pagesObject;

    public PdfInfo DocumentInfo { get; } = new();
    
    public PdfDocument(string path)
    {
        output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        pagesObject = 1;
        objectOffsets.Add(0);
    }

    public void Begin()
    {
        WriteHeader();
        AddFonts();
    }

    public void End()
    {
        WriteFinalObjects();
        var xref = WriteXRefTable();
        WriteTrailer(xref);
        output.Close();
    }

    public PdfFont GetFont(string name)
    {
        return new PdfFont(fontObjects[name]);
    }

    public void AddPage(PdfPage page)
    {
        List<Byte> contents = page.GetContents();

        List<Byte> bytes = new(Encoding.ASCII.GetBytes($"<< /Length {contents.Count} >>\nstream\n"));
        bytes.AddRange(contents);
        bytes.AddRange(Encoding.ASCII.GetBytes("endstream\n"));

        int stream = AppendObject(bytes.ToArray());

        Byte[] pageBytes = Encoding.ASCII.GetBytes($"<< /Type /Page /Parent {pagesObject} 0 R /MediaBox [{page.MediaBox}] /Contents {stream} 0 R >>\n");
        pageObjects.Add(AppendObject(pageBytes));
    }

    internal static string Escaped(string orig)
    {
        StringBuilder sb = new();
        foreach (char ch in orig)
            if (ch == '(' || ch == ')' || ch == '\\')
            {
                sb.Append('\\');
                sb.Append(ch);
            }
            else
            {
                sb.Append(ch);
            }
        return sb.ToString();
    }

    internal int AppendObject(byte[] bytes)
    {
        int number = objectOffsets.Count + 1;
        long offset = WriteObject(bytes, number);
        objectOffsets.Add(offset);
        return number;
    }

    internal int AppendStreamObject(byte[] stream)
    {
        List<Byte> bytes = new(Encoding.ASCII.GetBytes($"<< /Length {stream.Length} >>\nstream\n"));
        bytes.AddRange(stream);
        bytes.AddRange(Encoding.ASCII.GetBytes("endstream\n"));

        return AppendObject(bytes.ToArray());
    }

    long WriteObject(byte[] bytes, int number)
    {
        long offset = output.Position;
        byte[] obj = Encoding.ASCII.GetBytes($"{number} 0 obj\n");
        output.Write(obj);
        output.Write(bytes);
        obj = Encoding.ASCII.GetBytes($"endobj\n");
        output.Write(obj);
        return offset;
    }
    
    void WriteFinalObjects()
    {
        WritePageTree();
        infoObject = WriteInfo();
        rootObject = WriteRootCatalog();
    }

    void WriteHeader()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("%PDF-1.7\n%😃😃\n");
        output.Write(bytes);
    }

    void AddFonts()
    {
        AddStandardFont(PdfFont.Helvetica);
        AddStandardFont(PdfFont.HelveticaBold);
        AddStandardFont(PdfFont.HelveticaOblique);
        AddStandardFont(PdfFont.HelveticaBoldOblique);
    }

    void AddStandardFont(string name)
    {
        int obj = AppendObject(Encoding.ASCII.GetBytes($"<< /Type /Font /Subtype /Type1 /BaseFont /{name} >>\n"));
        fontObjects[name] = obj;
    }

    long WriteXRefTable()
    {
        long offset = output.Position;
        byte[] bytes = Encoding.ASCII.GetBytes($"xref\n0 {objectOffsets.Count+1}\n0000000000 65535 f \n");
        output.Write(bytes);
        foreach (var o in objectOffsets)
        {
            bytes = Encoding.ASCII.GetBytes($"{o:0000000000} 00000 n \n");
            output.Write(bytes);
        }
        return offset;
    }

    void WriteTrailer(long xrefOffset)
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"trailer << /Size {objectOffsets.Count+1} /Root {rootObject} 0 R /Info {infoObject} 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n");
        output.Write(bytes);
    }

    void WritePageTree()
    {
        StringBuilder sb = new();
        sb.Append("<< /Type /Pages\n/Kids [ ");
        foreach (var p in pageObjects)
            sb.Append($"{p} 0 R ");
        sb.Append($"] /Count {pageObjects.Count}\n/Resources << /Font << ");
        foreach (var f in fontObjects.Values)
            sb.Append($"/F{f} {f} 0 R ");
        sb.Append($">> >> >>\n");

        byte[] bytes = Encoding.ASCII.GetBytes(sb.ToString());
        long offset = WriteObject(bytes, pagesObject);
        objectOffsets[pagesObject - 1] = offset;
    }

    void AppendDictionaryString(List<Byte> bytes, string name, string? content)
    {
        if (!string.IsNullOrEmpty(content))
        {
            bytes.AddRange(Encoding.ASCII.GetBytes($"/{name} ("));
            // bytes.AddRange(Encoding.BigEndianUnicode.GetPreamble());
            // bytes.AddRange(Encoding.BigEndianUnicode.GetBytes(Escaped(content)));
            bytes.AddRange(Encoding.ASCII.GetBytes(Escaped(content)));
            bytes.AddRange(Encoding.ASCII.GetBytes($")\n"));
        }
    }

    int WriteInfo()
    {
        List<Byte> bytes = new();

        bytes.AddRange(Encoding.ASCII.GetBytes("<<\n"));
        AppendDictionaryString(bytes, "Title", DocumentInfo.Title);
        AppendDictionaryString(bytes, "Author", DocumentInfo.Author);
        AppendDictionaryString(bytes, "Subject", DocumentInfo.Subject);
        AppendDictionaryString(bytes, "Keywords", DocumentInfo.Keywords);
        AppendDictionaryString(bytes, "Producer", "CrosswordMaker CLI");

        string date = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        bytes.AddRange(Encoding.ASCII.GetBytes($"/CreationDate (D:{date}Z)\n>>\n"));
        return AppendObject(bytes.ToArray());
    }

    int WriteRootCatalog()
    {
        byte[] bytes = Encoding.ASCII.GetBytes($"<< /Type /Catalog /Pages {pagesObject} 0 R >>\n");
        return AppendObject(bytes);
    }

}
