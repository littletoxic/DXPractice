// Adapted from the Radiance HDR loading path in stb_image.h v2.30
// by Sean Barrett and contributors: https://github.com/nothings/stb
// stb_image.h is available under either:
// - MIT License, Copyright (c) 2017 Sean Barrett
// - Public Domain (www.unlicense.org)
using System.Buffers;

namespace DXDemo14;

internal readonly record struct HdrImageInfo(int Width, int Height, int SourceChannels, string Format, string Layout) {

    public bool UsesScanlineRle => Width is >= HdrImageLoader.MinScanlineWidthForRle and < HdrImageLoader.MaxScanlineWidthForRle;
}

internal sealed class HdrImage(int width, int height, int channels, float[] data) {
    public int Width { get; } = width;

    public int Height { get; } = height;

    public int Channels { get; } = channels;

    public float[] Data { get; } = data;
}

internal sealed class HdrLoadOptions {

    public int RequestedChannels { get; init; }

    public bool FlipVertically { get; init; }

    public int MaxDimension { get; init; } = HdrImageLoader.DefaultMaxDimension;
}

internal static class HdrImageLoader {

    internal const int DefaultMaxDimension = 1 << 24;
    internal const int MinScanlineWidthForRle = 8;
    internal const int MaxScanlineWidthForRle = 32768;

    private const int SourceChannels = 3;
    private const string SupportedFormat = "32-bit_rle_rgbe";
    private const string SupportedLayout = "-Y +X";

    public static bool IsHdr(string path) {
        return IsHdr(File.ReadAllBytes(path));
    }

    public static bool IsHdr(ReadOnlySpan<byte> data) {
        var reader = new HdrByteReader(data);
        return TryReadSignature(ref reader);
    }

    public static HdrImageInfo ReadInfo(string path) {
        return ReadInfo(File.ReadAllBytes(path));
    }

    public static HdrImageInfo ReadInfo(ReadOnlySpan<byte> data) {
        if (!TryReadInfo(data, out var info, out var error)) {
            throw new InvalidDataException(error);
        }

        return info;
    }

    public static bool TryReadInfo(ReadOnlySpan<byte> data, out HdrImageInfo info, out string error) {
        try {
            var reader = new HdrByteReader(data);
            info = ParseInfo(ref reader, DefaultMaxDimension);
            error = null;
            return true;
        } catch (Exception ex) when (ex is InvalidDataException or OverflowException) {
            info = default;
            error = ex.Message;
            return false;
        }
    }

    public static HdrImage Load(string path, HdrLoadOptions options = null) {
        return Load(File.ReadAllBytes(path), options);
    }

    public static HdrImage Load(ReadOnlySpan<byte> data, HdrLoadOptions options = null) {
        options ??= new HdrLoadOptions();
        ValidateOptions(options);

        var reader = new HdrByteReader(data);
        var info = ParseInfo(ref reader, options.MaxDimension);
        int channels = options.RequestedChannels == 0 ? info.SourceChannels : options.RequestedChannels;
        int pixelCount = checked(info.Width * info.Height);
        float[] decodedData = new float[checked(pixelCount * channels)];

        DecodeImage(ref reader, info, channels, decodedData);

        if (options.FlipVertically) {
            VerticalFlip(decodedData, info.Width, info.Height, channels);
        }

        return new HdrImage(info.Width, info.Height, channels, decodedData);
    }

    public static bool TryLoad(string path, out HdrImage image, out string error, HdrLoadOptions options = null) {
        try {
            image = Load(path, options);
            error = null;
            return true;
        } catch (Exception ex) when (ex is IOException or InvalidDataException or OverflowException or ArgumentOutOfRangeException) {
            image = null;
            error = ex.Message;
            return false;
        }
    }

    public static bool TryLoad(ReadOnlySpan<byte> data, out HdrImage image, out string error, HdrLoadOptions options = null) {
        try {
            image = Load(data, options);
            error = null;
            return true;
        } catch (Exception ex) when (ex is InvalidDataException or OverflowException or ArgumentOutOfRangeException) {
            image = null;
            error = ex.Message;
            return false;
        }
    }

    private static void ValidateOptions(HdrLoadOptions options) {
        if (options.RequestedChannels is < 0 or > 4) {
            throw new ArgumentOutOfRangeException(nameof(options), "RequestedChannels must be between 0 and 4.");
        }

        if (options.MaxDimension <= 0) {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxDimension must be positive.");
        }
    }

    private static HdrImageInfo ParseInfo(ref HdrByteReader reader, int maxDimension) {
        if (!TryReadSignature(ref reader)) {
            throw new InvalidDataException("Not a Radiance HDR image.");
        }

        bool hasFormat = false;

        while (reader.TryReadLine(out var line)) {
            if (line.Length == 0) {
                break;
            }

            if (line.SequenceEqual("FORMAT=32-bit_rle_rgbe"u8)) {
                hasFormat = true;
            }
        }

        if (!hasFormat) {
            throw new InvalidDataException("Unsupported HDR format.");
        }

        if (!reader.TryReadLine(out var resolutionLine)) {
            throw new InvalidDataException("Missing HDR resolution line.");
        }

        if (!TryParseResolution(resolutionLine, out int width, out int height)) {
            throw new InvalidDataException("Unsupported HDR data layout.");
        }

        if (width > maxDimension || height > maxDimension) {
            throw new InvalidDataException("HDR image is too large.");
        }

        checked {
            _ = width * height * SourceChannels * sizeof(float);
        }

        return new HdrImageInfo(width, height, SourceChannels, SupportedFormat, SupportedLayout);
    }

    private static bool TryReadSignature(ref HdrByteReader reader) {
        if (!reader.TryReadLine(out var line)) {
            return false;
        }

        return line.SequenceEqual("#?RADIANCE"u8) || line.SequenceEqual("#?RGBE"u8);
    }

    private static bool TryParseResolution(ReadOnlySpan<byte> line, out int width, out int height) {
        width = 0;
        height = 0;

        line = TrimAsciiWhitespace(line);

        if (!TryReadToken(ref line, out var token) || !token.SequenceEqual("-Y"u8)) {
            return false;
        }

        if (!TryReadIntToken(ref line, out height)) {
            return false;
        }

        if (!TryReadToken(ref line, out token) || !token.SequenceEqual("+X"u8)) {
            return false;
        }

        if (!TryReadIntToken(ref line, out width)) {
            return false;
        }

        return TrimAsciiWhitespace(line).Length == 0;
    }

    private static bool TryReadIntToken(ref ReadOnlySpan<byte> source, out int value) {
        value = 0;

        if (!TryReadToken(ref source, out var token)) {
            return false;
        }

        return TryParsePositiveInt(token, out value);
    }

    private static bool TryReadToken(ref ReadOnlySpan<byte> source, out ReadOnlySpan<byte> token) {
        source = TrimAsciiWhitespace(source);

        if (source.IsEmpty) {
            token = default;
            return false;
        }

        int i = 0;
        while (i < source.Length && !IsAsciiWhitespace(source[i])) {
            i++;
        }

        token = source[..i];
        source = source[i..];
        return true;
    }

    private static bool TryParsePositiveInt(ReadOnlySpan<byte> token, out int value) {
        value = 0;

        if (token.IsEmpty) {
            return false;
        }

        foreach (byte b in token) {
            if (b is < (byte)'0' or > (byte)'9') {
                return false;
            }

            checked {
                value = value * 10 + (b - '0');
            }
        }

        return value > 0;
    }

    private static ReadOnlySpan<byte> TrimAsciiWhitespace(ReadOnlySpan<byte> value) {
        int start = 0;
        while (start < value.Length && IsAsciiWhitespace(value[start])) {
            start++;
        }

        int end = value.Length - 1;
        while (end >= start && IsAsciiWhitespace(value[end])) {
            end--;
        }

        return value[start..(end + 1)];
    }

    private static bool IsAsciiWhitespace(byte value) {
        return value is (byte)' ' or (byte)'\t' or (byte)'\r';
    }

    private static void DecodeImage(ref HdrByteReader reader, HdrImageInfo info, int requestedChannels, float[] destination) {
        if (!info.UsesScanlineRle) {
            DecodeFlatPixels(ref reader, info.Width, info.Height, requestedChannels, destination, 0);
            return;
        }

        int scanlineByteCount = checked(info.Width * 4);
        byte[] rented = null;
        Span<byte> scanline = scanlineByteCount <= 1024
            ? stackalloc byte[scanlineByteCount]
            : (rented = ArrayPool<byte>.Shared.Rent(scanlineByteCount));
        scanline = scanline[..scanlineByteCount];

        try {
            for (int row = 0; row < info.Height; row++) {
                byte c1 = reader.ReadByteOrThrow("Unexpected end of HDR data.");
                byte c2 = reader.ReadByteOrThrow("Unexpected end of HDR data.");
                byte lenHigh = reader.ReadByteOrThrow("Unexpected end of HDR data.");

                if (c1 != 2 || c2 != 2 || (lenHigh & 0x80) != 0) {
                    Span<byte> firstPixel = [
                        c1,
                        c2,
                        lenHigh,
                        reader.ReadByteOrThrow("Unexpected end of HDR data."),
                    ];

                    ConvertRgbeToFloat(firstPixel, destination, 0, requestedChannels);
                    DecodeFlatPixels(ref reader, info.Width, info.Height, requestedChannels, destination, 1);
                    return;
                }

                int encodedWidth = (lenHigh << 8) | reader.ReadByteOrThrow("Unexpected end of HDR data.");
                if (encodedWidth != info.Width) {
                    throw new InvalidDataException("Invalid decoded HDR scanline length.");
                }

                for (int channel = 0; channel < 4; channel++) {
                    int x = 0;
                    while (x < info.Width) {
                        byte count = reader.ReadByteOrThrow("Unexpected end of HDR data.");
                        int remaining = info.Width - x;

                        if (count > 128) {
                            int runLength = count - 128;
                            if (runLength == 0 || runLength > remaining) {
                                throw new InvalidDataException("Bad RLE data in HDR image.");
                            }

                            byte value = reader.ReadByteOrThrow("Unexpected end of HDR data.");
                            for (int i = 0; i < runLength; i++) {
                                scanline[(x++ * 4) + channel] = value;
                            }
                        } else {
                            int literalLength = count;
                            if (literalLength == 0 || literalLength > remaining) {
                                throw new InvalidDataException("Bad RLE data in HDR image.");
                            }

                            for (int i = 0; i < literalLength; i++) {
                                scanline[(x++ * 4) + channel] = reader.ReadByteOrThrow("Unexpected end of HDR data.");
                            }
                        }
                    }
                }

                int rowOffset = checked(row * info.Width * requestedChannels);
                for (int x = 0; x < info.Width; x++) {
                    ConvertRgbeToFloat(scanline[(x * 4)..((x * 4) + 4)], destination, rowOffset + (x * requestedChannels), requestedChannels);
                }
            }
        } finally {
            if (rented is not null) {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }
    }

    private static void DecodeFlatPixels(ref HdrByteReader reader, int width, int height, int requestedChannels, float[] destination, int startPixelIndex) {
        Span<byte> rgbe = stackalloc byte[4];
        int pixelCount = checked(width * height);

        for (int pixelIndex = startPixelIndex; pixelIndex < pixelCount; pixelIndex++) {
            reader.ReadExactly(rgbe, "Unexpected end of HDR data.");
            ConvertRgbeToFloat(rgbe, destination, pixelIndex * requestedChannels, requestedChannels);
        }
    }

    private static void ConvertRgbeToFloat(ReadOnlySpan<byte> input, float[] output, int outputIndex, int requestedChannels) {
        if (input[3] != 0) {
            float scale = MathF.ScaleB(1.0f, input[3] - (128 + 8));
            if (requestedChannels <= 2) {
                output[outputIndex] = (input[0] + input[1] + input[2]) * scale / 3.0f;
            } else {
                output[outputIndex] = input[0] * scale;
                output[outputIndex + 1] = input[1] * scale;
                output[outputIndex + 2] = input[2] * scale;
            }

            if (requestedChannels == 2) {
                output[outputIndex + 1] = 1.0f;
            } else if (requestedChannels == 4) {
                output[outputIndex + 3] = 1.0f;
            }
        } else {
            switch (requestedChannels) {
                case 4:
                    output[outputIndex + 3] = 1.0f;
                    goto case 3;
                case 3:
                    output[outputIndex] = 0.0f;
                    output[outputIndex + 1] = 0.0f;
                    output[outputIndex + 2] = 0.0f;
                    break;
                case 2:
                    output[outputIndex + 1] = 1.0f;
                    goto case 1;
                case 1:
                    output[outputIndex] = 0.0f;
                    break;
            }
        }
    }

    private static void VerticalFlip(float[] image, int width, int height, int channels) {
        int rowLength = checked(width * channels);
        float[] rowBuffer = new float[rowLength];
        Span<float> swapBuffer = rowBuffer;

        for (int top = 0, bottom = height - 1; top < bottom; top++, bottom--) {
            Span<float> topRow = image.AsSpan(top * rowLength, rowLength);
            Span<float> bottomRow = image.AsSpan(bottom * rowLength, rowLength);
            topRow.CopyTo(swapBuffer);
            bottomRow.CopyTo(topRow);
            swapBuffer.CopyTo(bottomRow);
        }
    }

    private ref struct HdrByteReader {

        private readonly ReadOnlySpan<byte> _data;
        private int _position;

        public HdrByteReader(ReadOnlySpan<byte> data) {
            _data = data;
            _position = 0;
        }

        public bool TryReadLine(out ReadOnlySpan<byte> line) {
            if (_position >= _data.Length) {
                line = default;
                return false;
            }

            int start = _position;
            while (_position < _data.Length && _data[_position] != (byte)'\n') {
                _position++;
            }

            int end = _position;
            if (_position < _data.Length) {
                _position++;
            }

            line = _data[start..end];
            if (!line.IsEmpty && line[^1] == (byte)'\r') {
                line = line[..^1];
            }

            return true;
        }

        public byte ReadByteOrThrow(string message) {
            if (_position >= _data.Length) {
                throw new InvalidDataException(message);
            }

            return _data[_position++];
        }

        public void ReadExactly(scoped Span<byte> destination, string message) {
            if (_position > _data.Length - destination.Length) {
                throw new InvalidDataException(message);
            }

            _data.Slice(_position, destination.Length).CopyTo(destination);
            _position += destination.Length;
        }
    }
}
