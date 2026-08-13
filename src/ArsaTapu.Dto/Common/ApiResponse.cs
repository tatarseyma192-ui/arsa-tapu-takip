namespace ArsaTapu.Dto.Common;

/// <summary>
/// Technical Defaults madde 4'teki standart response zarfı.
/// Teknik hata detayı (stack trace vb.) asla Message alanına yazılmamalıdır.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<FieldError>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, List<FieldError>? errors = null) =>
        new() { Success = false, Data = default, Message = message, Errors = errors };
}

/// <summary>Data alanı gerekmeyen yanıtlar için (ör. silme işlemleri).</summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Message { get; set; }
    public List<FieldError>? Errors { get; set; }

    public static ApiResponse Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, List<FieldError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
