﻿using PostSharp.Aspects;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Serialization;

namespace AopHelper
{
    [OnMethodBoundaryAspectConfiguration(SerializerType = typeof(MsilAspectSerializer))]
    public class ExceptionAspect : OnExceptionAspect
    {
        public override void OnException(MethodExecutionArgs args)
        {
            if (Exceptions.Handle(args.Exception))
                args.FlowBehavior = FlowBehavior.Continue;
            args.FlowBehavior = FlowBehavior.ThrowException;
            LoggingHelper.Writelog("exception:" + args.Exception);
        }
    }
}
