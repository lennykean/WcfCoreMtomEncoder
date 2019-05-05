using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WcfCoreMtomEncoder
{
    public class MtomMessageEncoder : MessageEncoder
    {
        private readonly MessageEncoder _innerEncoder;

        public MtomMessageEncoder(MessageEncoder innerEncoder)
        {
            _innerEncoder = innerEncoder;
        }

        public override string ContentType => _innerEncoder.ContentType;
        public override string MediaType => _innerEncoder.MediaType;
        public override MessageVersion MessageVersion => _innerEncoder.MessageVersion;

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            using (var stream = new MemoryStream(buffer.ToArray()))
            {
                var message = ReadMessage(stream, 1024, contentType);
                bufferManager.ReturnBuffer(buffer.Array);
                return message;
            }
        }
        
        public override Message ReadMessage(Stream stream, int maxSizeOfHeaders, string contentType)
        {
            var parts = (
                from p in GetMultipartContent(stream, contentType)
                select new MtomPart(p)).ToList();

            var mainPart = (
                from part in parts
                where part.ContentId == new ContentType(contentType).Parameters?["start"]
                select part).SingleOrDefault() ?? parts.First();

            var mainContent = ResolveRefs(mainPart.GetStringContentForEncoder(_innerEncoder), parts);
            var mainContentStream = CreateStream(mainContent, mainPart.ContentType);

            return _innerEncoder.ReadMessage(mainContentStream, maxSizeOfHeaders, mainPart.ContentType.ToString());
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            return _innerEncoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
        }

        public override void WriteMessage(Message message, Stream stream)
        {
            _innerEncoder.WriteMessage(message, stream);
        }

        public override bool IsContentTypeSupported(string contentType)
        {
            if (_innerEncoder.IsContentTypeSupported(contentType))
                return true;

            var contentTypes = contentType.Split(';').Select(c => c.Trim()).ToList();
            
            if (contentTypes.Contains("multipart/related", StringComparer.OrdinalIgnoreCase) &&
                contentTypes.Contains("type=\"application/xop+xml\"", StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public override T GetProperty<T>()
        {
            return _innerEncoder.GetProperty<T>();
        }

        private static IEnumerable<HttpContent> GetMultipartContent(Stream stream, string contentType)
        {
            var content = new StreamContent(stream);

            content.Headers.Add("Content-Type", contentType);

            return content.ReadAsMultipartAsync().Result.Contents;
        }

        private static string ResolveRefs(string mainContent, IList<MtomPart> parts)
        {
            bool ReferenceMatch(XAttribute hrefAttr, MtomPart part)
            {
                var partId = Regex.Match(part.ContentId, "<(?<uri>.*)>");
                var href = Regex.Match(hrefAttr.Value, "cid:(?<uri>.*)");

                return href.Groups["uri"].Value == partId.Groups["uri"].Value;
            }

            var doc = XDocument.Parse(mainContent);
            var references = doc.Descendants(XName.Get("Include", "http://www.w3.org/2004/08/xop/include")).ToList();

            foreach (var reference in references)
            {
                var referencedPart = (
                    from part in parts
                    where ReferenceMatch(reference.Attribute("href"), part)
                    select part).Single();

                reference.ReplaceWith(Convert.ToBase64String(referencedPart.GetRawContent()));
            }
            return doc.ToString(SaveOptions.DisableFormatting);
        }

        private static Stream CreateStream(string content, MediaTypeHeaderValue contentType)
        {
            var encoding = !string.IsNullOrEmpty(contentType.CharSet)
                ? Encoding.GetEncoding(contentType.CharSet)
                : Encoding.Default;

            return new MemoryStream(encoding.GetBytes(content));
        }
    }
}