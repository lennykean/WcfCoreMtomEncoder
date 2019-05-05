using System;
using System.ServiceModel.Channels;

namespace WcfCoreMtomEncoder
{
    public class MtomMessageEncoderBindingElement : MessageEncodingBindingElement
    {
        private readonly MessageEncodingBindingElement _innerEncodingBindingElement;
        
        public MtomMessageEncoderBindingElement(MessageEncodingBindingElement innerEncodingBindingElement)
        {
            _innerEncodingBindingElement = innerEncodingBindingElement;
        }

        public override BindingElement Clone()
        {
            return new MtomMessageEncoderBindingElement(_innerEncodingBindingElement);
        }

        public override MessageEncoderFactory CreateMessageEncoderFactory()
        {
            return new MtomMessageEncoderFactory(_innerEncodingBindingElement.CreateMessageEncoderFactory());
        }

        public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.BindingParameters.Add(this);

            return context.BuildInnerChannelFactory<TChannel>();
        }

        public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
        {
            return _innerEncodingBindingElement.CanBuildChannelFactory<TChannel>(context);
        }

        public override T GetProperty<T>(BindingContext context)
        {
            return _innerEncodingBindingElement.GetProperty<T>(context);
        }

        public override MessageVersion MessageVersion
        {
            get => _innerEncodingBindingElement.MessageVersion;
            set => _innerEncodingBindingElement.MessageVersion = value;
        }
    }
}