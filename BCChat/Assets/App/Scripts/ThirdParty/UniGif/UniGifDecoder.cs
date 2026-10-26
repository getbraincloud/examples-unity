/*
UniGif
Copyright (c) 2015 WestHillApps (Hironari Nishioka)
This software is released under the MIT License.
http://opensource.org/licenses/mit-license.php
*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static partial class UniGif
{
    // If true, disposal method 2 regions are cleared to transparent instead of the logical screen background color.
    // This eliminates persistent solid color boxes behind partially transparent animations.
    private const bool ForceDisposeToTransparent = true;

    /// <summary>
    /// Decode to textures from GIF data
    /// </summary>
    /// <param name="gifData">GIF data</param>
    /// <param name="callback">Callback method(param is GIF texture list)</param>
    /// <param name="filterMode">Textures filter mode</param>
    /// <param name="wrapMode">Textures wrap mode</param>
    /// <returns>IEnumerator</returns>
    private static IEnumerator DecodeTextureCoroutine(GifData gifData, Action<List<GifTexture>> callback,
        FilterMode filterMode, TextureWrapMode wrapMode, Action<GifTexture> frameDecoded,
        int maxWidth, int maxHeight)
    {
        if (gifData.m_imageBlockList == null || gifData.m_imageBlockList.Count < 1)
        {
            yield break;
        }

        List<GifTexture> gifTexList = new List<GifTexture>(gifData.m_imageBlockList.Count);
        List<ushort> disposalMethodList = new List<ushort>(gifData.m_imageBlockList.Count);

        int imgIndex = 0;
        ImageBlock? prevImageBlock = null;
        long sliceStarted = System.Diagnostics.Stopwatch.GetTimestamp();
        long sliceBudget = Math.Max(1L, System.Diagnostics.Stopwatch.Frequency / 120L);
        float outputScale = 1f;
        if (maxWidth > 0) outputScale = Math.Min(outputScale, (float)maxWidth / gifData.m_logicalScreenWidth);
        if (maxHeight > 0) outputScale = Math.Min(outputScale, (float)maxHeight / gifData.m_logicalScreenHeight);
        int outputWidth = Math.Max(1, (int)(gifData.m_logicalScreenWidth * outputScale));
        int outputHeight = Math.Max(1, (int)(gifData.m_logicalScreenHeight * outputScale));

        for (int i = 0; i < gifData.m_imageBlockList.Count; i++)
        {
            ImageBlock imageBlock = gifData.m_imageBlockList[i];
            byte[] decodedData = GetDecodedData(imageBlock);

            GraphicControlExtension? graphicCtrlEx = GetGraphicCtrlExt(gifData, imgIndex);

            int transparentIndex = GetTransparentIndex(graphicCtrlEx);

            disposalMethodList.Add(GetDisposalMethod(graphicCtrlEx));

            Color32 bgColor;
            List<byte[]> colorTable = GetColorTableAndSetBgColor(gifData, imageBlock, transparentIndex, out bgColor);

            bool filledTexture;
            Texture2D tex = CreateTexture2D(gifData, gifTexList, imgIndex, disposalMethodList, bgColor,
                filterMode, wrapMode, outputWidth, outputHeight, out filledTexture, prevImageBlock,
                out Color32[] pixelBuffer);

            // Compose frame into pixelBuffer (single upload after loop)
            // Reverse set pixels. because GIF data starts from the top left.
            for (int y = tex.height - 1; y >= 0; y--)
            {
                WriteTexturePixelRow(pixelBuffer, tex.width, tex.height, y,
                    gifData.m_logicalScreenWidth, gifData.m_logicalScreenHeight,
                    imageBlock, decodedData, colorTable, bgColor, transparentIndex, filledTexture);
            }

            tex.SetPixels32(pixelBuffer);
            tex.Apply();

            float delaySec = GetDelaySec(graphicCtrlEx);

            // Add to GIF texture list
            GifTexture gifTexture = new GifTexture(tex, delaySec);
            gifTexList.Add(gifTexture);
            frameDecoded?.Invoke(gifTexture);

            prevImageBlock = imageBlock;
            imgIndex++;

            // Yield only when the decoder has consumed its real per-frame
            // time budget. The original implementation always waited three
            // rendered frames for every GIF frame, causing long artificial
            // delays even when decoding was fast.
            if (System.Diagnostics.Stopwatch.GetTimestamp() - sliceStarted >= sliceBudget)
            {
                yield return null;
                sliceStarted = System.Diagnostics.Stopwatch.GetTimestamp();
            }
        }

        if (callback != null)
        {
            callback(gifTexList);
        }

        yield break;
    }

    #region Call from DecodeTexture methods

    /// <summary>
    /// Get decoded image data from ImageBlock
    /// </summary>
    private static byte[] GetDecodedData(ImageBlock imgBlock)
    {
        int compressedLength = 0;
        for (int i = 0; i < imgBlock.m_imageDataList.Count; i++)
        {
            compressedLength += imgBlock.m_imageDataList[i].m_imageData.Length;
        }

        byte[] lzwData = new byte[compressedLength];
        int compressedOffset = 0;
        for (int i = 0; i < imgBlock.m_imageDataList.Count; i++)
        {
            byte[] block = imgBlock.m_imageDataList[i].m_imageData;
            Buffer.BlockCopy(block, 0, lzwData, compressedOffset, block.Length);
            compressedOffset += block.Length;
        }

        // LZW decode
        int needDataSize = imgBlock.m_imageHeight * imgBlock.m_imageWidth;
        byte[] decodedData = DecodeGifLZW(lzwData, imgBlock.m_lzwMinimumCodeSize, needDataSize);

        // Sort interlace GIF
        if (imgBlock.m_interlaceFlag)
        {
            decodedData = SortInterlaceGifData(decodedData, imgBlock.m_imageWidth);
        }
        return decodedData;
    }

    /// <summary>
    /// Get color table and set background color (local or global)
    /// </summary>
    private static List<byte[]> GetColorTableAndSetBgColor(GifData gifData, ImageBlock imgBlock, int transparentIndex, out Color32 bgColor)
    {
        List<byte[]> colorTable = imgBlock.m_localColorTableFlag ? imgBlock.m_localColorTable : gifData.m_globalColorTableFlag ? gifData.m_globalColorTable : null;

        if (colorTable != null && gifData.m_bgColorIndex < colorTable.Count)
        {
            // Set background color from color table
            byte[] bgRgb = colorTable[gifData.m_bgColorIndex];
            bgColor = new Color32(bgRgb[0], bgRgb[1], bgRgb[2], (byte)(transparentIndex == gifData.m_bgColorIndex ? 0 : 255));
        }
        else
        {
            // Default: fully transparent instead of opaque black to avoid flashes.
            bgColor = new Color32(0, 0, 0, 0);
        }

        return colorTable;
    }

    /// <summary>
    /// Get GraphicControlExtension from GifData
    /// </summary>
    private static GraphicControlExtension? GetGraphicCtrlExt(GifData gifData, int imgBlockIndex)
    {
        if (gifData.m_graphicCtrlExList != null && gifData.m_graphicCtrlExList.Count > imgBlockIndex)
        {
            return gifData.m_graphicCtrlExList[imgBlockIndex];
        }
        return null;
    }

    /// <summary>
    /// Get transparent color index from GraphicControlExtension
    /// </summary>
    private static int GetTransparentIndex(GraphicControlExtension? graphicCtrlEx)
    {
        int transparentIndex = -1;
        if (graphicCtrlEx != null && graphicCtrlEx.Value.m_transparentColorFlag)
        {
            transparentIndex = graphicCtrlEx.Value.m_transparentColorIndex;
        }
        return transparentIndex;
    }

    /// <summary>
    /// Get delay seconds from GraphicControlExtension
    /// </summary>
    private static float GetDelaySec(GraphicControlExtension? graphicCtrlEx)
    {
        // Get delay sec from GraphicControlExtension
        float delaySec = graphicCtrlEx != null ? graphicCtrlEx.Value.m_delayTime / 100f : (1f / 60f);
        if (delaySec <= 0f)
        {
            delaySec = 0.1f;
        }
        return delaySec;
    }

    /// <summary>
    /// Get disposal method from GraphicControlExtension
    /// </summary>
    private static ushort GetDisposalMethod(GraphicControlExtension? graphicCtrlEx)
    {
        // Map 0 (unspecified) -> 1 (do not dispose)
        ushort method = graphicCtrlEx != null ? graphicCtrlEx.Value.m_disposalMethod : (ushort)2;
        if (method == 0)
        {
            method = 1;
        }

        return method;
    }

    /// <summary>
    /// Create Texture2D object and initial pixel buffer (no GPU upload yet)
    /// </summary>
    private static Texture2D CreateTexture2D(GifData gifData, List<GifTexture> gifTexList, int imgIndex,
        List<ushort> disposalMethodList, Color32 bgColor, FilterMode filterMode, TextureWrapMode wrapMode,
        int outputWidth, int outputHeight, out bool filledTexture, ImageBlock? prevImageBlock,
        out Color32[] pixelBuffer)
    {
        filledTexture = false;

        // Create texture
        Texture2D tex = new Texture2D(outputWidth, outputHeight, TextureFormat.ARGB32, false);
        tex.filterMode = filterMode;
        tex.wrapMode = wrapMode;

        pixelBuffer = new Color32[tex.width * tex.height];

        // Check dispose
        ushort prevDisposal = imgIndex > 0 ? disposalMethodList[imgIndex - 1] : (ushort)2;
        int useBeforeIndex = -1;

        if (imgIndex == 0)
        {
            // Initial canvas: fill either background color or transparent
            Color32 baseFill = ForceDisposeToTransparent
                ? new Color32(0, 0, 0, 0)
                : bgColor;
            for (int i = 0; i < pixelBuffer.Length; i++) pixelBuffer[i] = baseFill;
            filledTexture = true;
            return tex;
        }

        if (prevDisposal == 1)
        {
            // Do not dispose
            useBeforeIndex = imgIndex - 1;
        }
        else if (prevDisposal == 2)
        {
            // Restore to background
            useBeforeIndex = imgIndex - 1;
        }
        else if (prevDisposal == 3)
        {
            // 3 (Restore to previous)
            for (int i = imgIndex - 2; i >= 0; i--)
            {
                if (disposalMethodList[i] == 1)
                {
                    useBeforeIndex = i;
                    break;
                }
            }

            if (useBeforeIndex < 0)
            {
                Color32 fill = ForceDisposeToTransparent ? new Color32(0, 0, 0, 0) : bgColor;
                for (int i = 0; i < pixelBuffer.Length; i++) pixelBuffer[i] = fill;
                filledTexture = true;
                return tex;
            }
        }
        else
        {
            // Treat as restore to background
            Color32 fill = ForceDisposeToTransparent ? new Color32(0, 0, 0, 0) : bgColor;
            for (int i = 0; i < pixelBuffer.Length; i++) pixelBuffer[i] = fill;
            filledTexture = true;
            return tex;
        }

        if (useBeforeIndex >= 0)
        {
            filledTexture = true;
            Color32[] prevPix = gifTexList[useBeforeIndex].m_texture2d.GetPixels32();
            Array.Copy(prevPix, pixelBuffer, prevPix.Length);

            // Disposal 2: clear only previous frame rect
            if (prevDisposal == 2 && prevImageBlock.HasValue)
            {
                var prev = prevImageBlock.Value;
                Color32 clearColor = ForceDisposeToTransparent ? new Color32(0, 0, 0, 0) : bgColor;

                for (int y = 0; y < tex.height; y++)
                {
                    int sourceY = (tex.height - 1 - y) * gifData.m_logicalScreenHeight / tex.height;
                    if (sourceY < prev.m_imageTopPosition ||
                        sourceY >= prev.m_imageTopPosition + prev.m_imageHeight) continue;

                    int baseIndex = y * tex.width;
                    for (int x = 0; x < tex.width; x++)
                    {
                        int sourceX = x * gifData.m_logicalScreenWidth / tex.width;
                        if (sourceX >= prev.m_imageLeftPosition &&
                            sourceX < prev.m_imageLeftPosition + prev.m_imageWidth)
                        {
                            pixelBuffer[baseIndex + x] = clearColor;
                        }
                    }
                }
            }
        }

        return tex;
    }

    /// <summary>
    /// Write one texture row into pixel buffer (no immediate GPU call)
    /// </summary>
    private static void WriteTexturePixelRow(Color32[] pixels, int texWidth, int texHeight, int y,
        int sourceWidth, int sourceHeight, ImageBlock imgBlock, byte[] decodedData,
        List<byte[]> colorTable, Color32 bgColor, int transparentIndex, bool filledTexture)
    {
        int row = (texHeight - 1 - y) * sourceHeight / texHeight;

        for (int x = 0; x < texWidth; x++)
        {
            int sourceX = x * sourceWidth / texWidth;
            // Out of image blocks
            if (row < imgBlock.m_imageTopPosition ||
                row >= imgBlock.m_imageTopPosition + imgBlock.m_imageHeight ||
                sourceX < imgBlock.m_imageLeftPosition ||
                sourceX >= imgBlock.m_imageLeftPosition + imgBlock.m_imageWidth)
            {
                // Get pixel color from bg color
                if (filledTexture == false)
                {
                    pixels[y * texWidth + x] = ForceDisposeToTransparent ? new Color32(0, 0, 0, 0) : bgColor;
                }
                continue;
            }

            int dataIndex = (row - imgBlock.m_imageTopPosition) * imgBlock.m_imageWidth +
                            sourceX - imgBlock.m_imageLeftPosition;
            // Out of decoded data
            if (dataIndex < 0 || dataIndex >= decodedData.Length)
            {
                if (filledTexture == false)
                {
                    pixels[y * texWidth + x] = ForceDisposeToTransparent ? new Color32(0, 0, 0, 0) : bgColor;
                    Debug.LogError("dataIndex exceeded decodedData. index:" + dataIndex);
                }
                continue;
            }

            // Get pixel color from color table
            {
                byte colorIndex = decodedData[dataIndex];
                if (colorTable == null || colorTable.Count <= colorIndex)
                {
                    if (filledTexture == false)
                    {
                        pixels[y * texWidth + x] = ForceDisposeToTransparent ? new Color32(0, 0, 0, 0) : bgColor;
                        if (colorTable == null)
                        {
                            Debug.LogError("colorIndex exceeded the size of colorTable. colorTable is null. colorIndex:" + colorIndex);
                        }
                        else
                        {
                            Debug.LogError("colorIndex exceeded the size of colorTable. colorTable.Count:" + colorTable.Count + " colorIndex:" + colorIndex);
                        }
                    }
                    continue;
                }
                byte[] rgb = colorTable[colorIndex];

                // Set alpha
                bool isTransparent = transparentIndex >= 0 && transparentIndex == colorIndex;
                byte alpha = isTransparent ? (byte)0 : (byte)255;

                // If transparent and we already have previous composite -> keep underlying pixel
                if (!(filledTexture && isTransparent))
                {
                    pixels[y * texWidth + x] = new Color32(rgb[0], rgb[1], rgb[2], alpha);
                }
            }
        }
    }

    #endregion

    #region Decode LZW & Sort interrace methods

    /// <summary>
    /// GIF LZW decode
    /// </summary>
    /// <param name="compData">LZW compressed data</param>
    /// <param name="lzwMinimumCodeSize">LZW minimum code size</param>
    /// <param name="needDataSize">Need decoded data size</param>
    /// <returns>Decoded data array</returns>
    private static byte[] DecodeGifLZW(byte[] compData, int lzwMinimumCodeSize, int needDataSize)
    {
        if (needDataSize <= 0)
        {
            return Array.Empty<byte>();
        }
        lzwMinimumCodeSize = Math.Max(2, Math.Min(8, lzwMinimumCodeSize));

        int clearCode = 1 << lzwMinimumCodeSize;
        int endCode = clearCode + 1;
        int available = endCode + 1;
        int codeSize = lzwMinimumCodeSize + 1;
        int codeMask = (1 << codeSize) - 1;

        short[] prefix = new short[4096];
        byte[] suffix = new byte[4096];
        byte[] stack = new byte[4097];
        byte[] output = new byte[needDataSize];
        for (int i = 0; i < clearCode; i++)
        {
            suffix[i] = (byte)i;
        }

        int datum = 0;
        int bits = 0;
        int inputIndex = 0;
        int outputIndex = 0;
        int stackSize = 0;
        int oldCode = -1;
        byte first = 0;

        while (outputIndex < needDataSize)
        {
            if (stackSize == 0)
            {
                while (bits < codeSize)
                {
                    if (inputIndex >= compData.Length) return output;
                    datum |= compData[inputIndex++] << bits;
                    bits += 8;
                }

                int code = datum & codeMask;
                datum >>= codeSize;
                bits -= codeSize;

                if (code == clearCode)
                {
                    codeSize = lzwMinimumCodeSize + 1;
                    codeMask = (1 << codeSize) - 1;
                    available = endCode + 1;
                    oldCode = -1;
                    continue;
                }
                if (code == endCode || code > available) break;

                if (oldCode == -1)
                {
                    output[outputIndex++] = suffix[code];
                    first = suffix[code];
                    oldCode = code;
                    continue;
                }

                int inputCode = code;
                if (code == available)
                {
                    stack[stackSize++] = first;
                    code = oldCode;
                }

                int chainGuard = 0;
                while (code >= clearCode && chainGuard++ < 4096)
                {
                    stack[stackSize++] = suffix[code];
                    code = prefix[code];
                }
                if (chainGuard >= 4096) break;

                first = suffix[code];
                stack[stackSize++] = first;

                if (available < 4096)
                {
                    prefix[available] = (short)oldCode;
                    suffix[available] = first;
                    available++;
                    if ((available & codeMask) == 0 && available < 4096)
                    {
                        codeSize++;
                        codeMask = (1 << codeSize) - 1;
                    }
                }
                oldCode = inputCode;
            }

            stackSize--;
            output[outputIndex++] = stack[stackSize];
        }

        return output;
    }

    /// <summary>
    /// Sort interlace GIF data
    /// </summary>
    /// <param name="decodedData">Decoded GIF data</param>
    /// <param name="xNum">Pixel number of horizontal row</param>
    /// <returns>Sorted data</returns>
    private static byte[] SortInterlaceGifData(byte[] decodedData, int xNum)
    {
        int rowNo = 0;
        int dataIndex = 0;
        var newArr = new byte[decodedData.Length];
        // Every 8th. row, starting with row 0.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 == 0)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 8th. row, starting with row 4.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 == 4)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 4th. row, starting with row 2.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 4 == 2)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }
        rowNo = 0;
        // Every 2nd. row, starting with row 1.
        for (int i = 0; i < newArr.Length; i++)
        {
            if (rowNo % 8 != 0 && rowNo % 8 != 4 && rowNo % 4 != 2)
            {
                newArr[i] = decodedData[dataIndex];
                dataIndex++;
            }
            if (i != 0 && i % xNum == 0)
            {
                rowNo++;
            }
        }

        return newArr;
    }

    #endregion
}
