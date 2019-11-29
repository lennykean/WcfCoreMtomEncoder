using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;

namespace WcfCoreMtomEncoder
{
    internal class MtomPart
    {
        private readonly HttpContent _part;

        public MtomPart(HttpContent part)
        {
            _part = part;
        }

        public MediaTypeHeaderValue ContentType
        {
            get
            {
                string contentTypeHeaderValue = _part.Headers.GetValues("Content-Type").FirstOrDefault();

                MediaTypeHeaderValue parsedValue;
                if (!String.IsNullOrEmpty(contentTypeHeaderValue) && MediaTypeHeaderValue.TryParse(contentTypeHeaderValue.TrimEnd(';'), out parsedValue))
                    return parsedValue;

                return _part.Headers.ContentType;
            }
        }

        public string ContentTransferEncoding => _part.Headers.TryGetValues("Content-Transfer-Encoding", out var values) ? values.Single() : null;
        public string ContentId => _part.Headers.TryGetValues("Content-ID", out var values) ? values.Single() : null;

        public byte[] GetRawContent()
        {
            if (!Regex.IsMatch(ContentTransferEncoding, "((7|8)bit)|binary", RegexOptions.IgnoreCase))
                throw new NotSupportedException();

            return ReadFromStream();
        }

        public string GetStringContentForEncoder(MessageEncoder encoder)
        {
            if (ContentType == null || 
                !ContentType.Parameters.Any(p => p.Name == "type" && encoder.IsContentTypeSupported(p.Value.Replace("\"", ""))))
                throw new NotSupportedException();

            var encoding = ContentType.CharSet != null ? Encoding.GetEncoding(ContentType.CharSet) : Encoding.Default;

            return encoding.GetString(GetRawContent());
        }

        private byte[] ReadFromStream()
        {
            using (var buffer = new MemoryStream())
            {
                _part.ReadAsStreamAsync().Result.CopyTo(buffer);
                return buffer.ToArray();
            }
        }
    }
}