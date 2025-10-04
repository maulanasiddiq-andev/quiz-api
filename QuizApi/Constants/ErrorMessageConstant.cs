namespace QuizApi.Constants
{
    public static class ErrorMessageConstant
    {
        public const string InvalidLogin = "Email atau password tidak valid";
        public const string DataNotFound = "Data tidak ditemukan";
        public const string AccessNotAllowed = "Anda tidak memiliki akses untuk mengubah data ini";
        public const string ServerError = "Server sedang mengalami masalah, silakan coba lagi nanti";
        public const string MethodParameterNull = "Request parameter tidak sesuai";
        public const string ItemAlreadyChanged = "Data sudah diupdate, silakan refresh halaman";
    }
}