﻿using System;
using System.Collections.Generic;
using System.Linq;
using EntityFrameworkCore.Generator.Metadata.Parsing;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EntityFrameworkCore.Generator.Parsing
{
    public class ContextVisitor : CSharpSyntaxWalker
    {
        public ContextVisitor()
        {
            ContextBaseType = "DbContext";
            DataSetTypes = new HashSet<string> { "DbSet", "IDbSet" };
        }

        public string ContextBaseType { get; set; }

        public HashSet<string> DataSetTypes { get; set; }

        public ParsedContext ParsedContext { get; set; }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            var hasBaseType = node.BaseList
                .DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Any(i => i.Identifier.ValueText == ContextBaseType);

            if (hasBaseType)
            {
                var name = node.Identifier.Text;
                if (ParsedContext == null)
                    ParsedContext = new ParsedContext();

                ParsedContext.ContextClass = name;
            }

            base.VisitClassDeclaration(node);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            ParseProperty(node);
            base.VisitPropertyDeclaration(node);
        }

        private void ParseProperty(PropertyDeclarationSyntax node)
        {
            var returnType = node.Type
                .DescendantNodesAndSelf()
                .OfType<GenericNameSyntax>()
                .FirstOrDefault();

            // expecting generic return type with 1 argument
            if (returnType == null || returnType.TypeArgumentList.Arguments.Count != 1)
                return;

            var returnName = returnType.Identifier.ValueText;
            if (!DataSetTypes.Contains(returnName))
                return;

            var firstArgument = returnType
                .TypeArgumentList
                .Arguments
                .FirstOrDefault();

            // last identifier is class name
            var className = firstArgument
                .DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Select(s => s.Identifier.ValueText)
                .LastOrDefault();

            var propertyName = node.Identifier.ValueText;

            if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(propertyName))
                return;

            var entitySet = new ParsedEntitySet
            {
                EntityClass = className,
                ContextProperty = propertyName
            };
            ParsedContext.Properties.Add(entitySet);
        }
    }
}