// Licensed under the MIT License.
// Copyright (C) 2018 Jumar A. Macato, All Rights Reserved.

namespace PicView.Avalonia.AnimatedImage.Decoding;

[Serializable]
public class InvalidGifStreamException(string message) : Exception(message);

