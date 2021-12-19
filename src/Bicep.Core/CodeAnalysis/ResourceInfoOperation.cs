// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using Bicep.Core.Semantics.Metadata;

namespace Bicep.Core.CodeAnalysis
{
    public record ResourceInfoOperation(
        ResourceMetadata Metadata,
        IndexReplacementContext? IndexContext) : Operation
    {
        public override void Accept(IOperationVisitor visitor)
            => visitor.VisitResourceInfoOperation(this);
    }
}