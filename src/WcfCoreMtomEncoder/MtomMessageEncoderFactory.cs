using System.ServiceModel.Channels;

namespace WcfCoreMtomEncoder
{
    public class MtomMessageEncoderFactory : MessageEncoderFactory
    {
        private readonly MessageEncoderFactory _innerMessageEncoderFactory;

        public MtomMessageEncoderFactory(MessageEncoderFactory innerMessageEncoderFactory)
        {
            _innerMessageEncoderFactory = innerMessageEncoderFactory;
        }

        public override MessageEncoder CreateSessionEncoder()
        {
            return new MtomMessageEncoder(_innerMessageEncoderFactory.CreateSessionEncoder());
        }

        public override MessageEncoder Encoder => new MtomMessageEncoder(_innerMessageEncoderFactory.Encoder);

        public override MessageVersion MessageVersion => _innerMessageEncoderFactory.MessageVersion;
    }
}