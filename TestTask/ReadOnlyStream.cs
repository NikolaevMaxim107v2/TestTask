using System.IO;
using System.Text;
using TestTask;

public class ReadOnlyStream : IReadOnlyStream
{
    private readonly FileStream _fileStream;
    private readonly StreamReader _reader;

    public bool IsEof { get; private set; }

    public ReadOnlyStream(string fileFullPath)
    {
        _fileStream = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
        _reader = new StreamReader(_fileStream, Encoding.UTF8);
        IsEof = false;
    }

    public char ReadNextChar()
    {
        if (IsEof)
            throw new EndOfStreamException();

        int value = _reader.Read();
        if (value == -1)
        {
            IsEof = true;
            throw new EndOfStreamException();
        }

        return (char)value;
    }

    public void ResetPositionToStart()
    {
        _fileStream.Seek(0, SeekOrigin.Begin);
        _reader.DiscardBufferedData();
        IsEof = false;
    }

    public void Dispose()
    {
        _reader.Dispose();
        _fileStream.Dispose();
    }
}
