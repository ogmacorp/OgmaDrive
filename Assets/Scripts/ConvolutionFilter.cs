// -----------------------------------------------------------------------------
// OgmaDrive
// Copyright (c) 2017 Ogma Intelligent Systems Corp. All rights reserved.
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvolutionFilter {

    public static Texture2D Apply(Texture2D inputTexture, float[,] filter, float scale = 1.0f)
    {
        Texture2D targetTexture = new Texture2D(inputTexture.width, inputTexture.height, inputTexture.format, false);

        float blue, green, red;

        int filterWidth = filter.GetLength(0);
        int filterOffset = (filterWidth - 1) / 2;

        Color pixel = new Color(0.0f, 0.0f, 0.0f);

        // Blank top and bottom rows
        for (int offsetY = 0; offsetY < filterOffset; offsetY++)
        {
            for (int offsetX = 0; offsetX < targetTexture.width; offsetX++)
            {
                targetTexture.SetPixel(offsetX, offsetY, pixel);
                targetTexture.SetPixel(offsetX, targetTexture.height - offsetY - 1, pixel);
            }
        }

        // Blank left and right rows
        for (int offsetX = 0; offsetX < filterOffset; offsetX++)
        {
            for (int offsetY = 0; offsetY < targetTexture.height; offsetY++)
            {
                targetTexture.SetPixel(offsetX, offsetY, pixel);
                targetTexture.SetPixel(targetTexture.width - offsetX - 1, offsetY, pixel);
            }
        }

        for (int offsetY = filterOffset; offsetY < inputTexture.height - filterOffset; offsetY++)
        {
            for (int offsetX = filterOffset; offsetX < inputTexture.width - filterOffset; offsetX++)
            {
                blue = 0.0f;
                green = 0.0f;
                red = 0.0f;

                for (int filterY = -filterOffset; filterY <= filterOffset; filterY++)
                {
                    for (int filterX = -filterOffset; filterX <= filterOffset; filterX++)
                    {
                        pixel = inputTexture.GetPixel(offsetX + filterX, offsetY + filterY);

                        red += (float)(pixel.r) * filter[filterX + filterOffset, filterY + filterOffset];
                        green += (float)(pixel.g) * filter[filterX + filterOffset, filterY + filterOffset];
                        blue += (float)(pixel.r) * filter[filterX + filterOffset, filterY + filterOffset];
                    }
                }

                pixel.r = Mathf.Clamp(red * scale, 0, 255);
                pixel.g = Mathf.Clamp(green * scale, 0, 255);
                pixel.b = Mathf.Clamp(blue * scale, 0, 255);

                targetTexture.SetPixel(offsetX, offsetY, pixel);
            }
        }

        return targetTexture;
    }

    // Sharr operator
    public static float[,] Sharr3x3Horizontal
    {
        get {
            return new float[,] {
                {  3,  0,  -3, },
                { 10,  0, -10, },
                {  3,  0,  -3, }
            };
        }
    }
    public static float[,] Sharr3x3Vertical
    {
        get {
            return new float[,] {
                {  3,  10,  3, },
                {  0,   0,  0, },
                { -3, -10, -3, }
            };
        }
    }

    // Prewitt operator
    public static float[,] Prewitt3x3Horizontal
    {
        get {
            return new float[,] {
                { -1,  0,  1, },
                { -1,  0,  1, },
                { -1,  0,  1, }
            };
        }
    }
    public static float[,] Prewitt3x3Vertical
    {
        get {
            return new float[,] {
                {  1,  1,  1, },
                {  0,  0,  0, },
                { -1, -1, -1, }
            };
        }
    }

    // Sobel-Feldman operator
    public static float[,] Sobel3x3Horizontal
    {
        get {
            return new float[,] {
                {  1,  0, -1, },
                {  2,  0, -2, },
                {  1,  0, -1, }
            };
        }
    }
    public static float[,] Sobel3x3Vertical
    {
        get {
            return new float[,] {
                {  1,  2,  1, },
                {  0,  0,  0, },
                { -1, -2, -1, }
            };
        }
    }

    // Gaussian blur operator (sigma 1.0)
    public static float[,] GaussianBlur
    {
        get
        {
            return new float[,]
            {
                { 0.003765f, 0.015019f, 0.023792f, 0.015019f, 0.003765f },
                { 0.015019f, 0.059912f, 0.094907f, 0.059912f, 0.015019f },
                { 0.023792f, 0.094907f, 0.150342f, 0.094907f, 0.023792f },
                { 0.015019f, 0.059912f, 0.094907f, 0.059912f, 0.015019f },
                { 0.003765f, 0.015019f, 0.023792f, 0.015019f, 0.003765f }
            };
        }
    }
    public static float[,] GaussianBlurWeights(float sigma)
    {
        const int kernelRadius = 5;
        float sigmaSqr = sigma * sigma;
        float scale = 1.0f / (2.0f * Mathf.PI * sigmaSqr);
        float[,] weights = new float[kernelRadius, kernelRadius];

        for (int y = -kernelRadius / 2; y <= kernelRadius / 2; y++)
            for (int x = -kernelRadius / 2; x <= kernelRadius / 2; x++)
            {
                weights[x + kernelRadius / 2, y + kernelRadius / 2] =
                    scale * Mathf.Exp(-1.0f * (((x * x) + (y * y)) / sigmaSqr));
            }

        return weights;
    }
}
