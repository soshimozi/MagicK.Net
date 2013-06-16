﻿//=================================================================================================
// Copyright 2013 Dirk Lemstra <http://magick.codeplex.com/>
//
// Licensed under the ImageMagick License (the "License"); you may not use this file except in 
// compliance with the License. You may obtain a copy of the License at
//
//   http://www.imagemagick.org/script/license.php
//
// Unless required by applicable law or agreed to in writing, software distributed under the
// License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
// express or implied. See the License for the specific language governing permissions and
// limitations under the License.
//=================================================================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Magick.NET.FileGenerator
{
	//==============================================================================================
	internal sealed class XsdGenerator
	{
		//===========================================================================================
		private MagickNET _MagickNET;
		private QuantumDepth _Depth;
		private XDocument _Document;
		private XNamespace _Namespace = "http://www.w3.org/2001/XMLSchema";
		private XmlNamespaceManager _Namespaces;
		//===========================================================================================
		private XsdGenerator(QuantumDepth depth)
		{
			_Depth = depth;
			_MagickNET = new MagickNET(depth);

			_Namespaces = new XmlNamespaceManager(new NameTable());
			_Namespaces.AddNamespace("xs", _Namespace.ToString());
		}
		//===========================================================================================
		private void AddArguments(XElement element, IEnumerable<MethodBase> methods)
		{
			string[] requiredParameters = (from method in methods
													 from parameter in method.GetParameters()
													 group parameter by parameter.Name into g
													 where g.Count() == methods.Count()
													 select g.Key).ToArray();

			ParameterInfo[] parameters = (from method in methods
													from parameter in method.GetParameters()
													select parameter).DistinctBy(p => p.Name).ToArray();

			AddParameterElements(element, parameters, requiredParameters);
			AddParameterAttributes(element, parameters, requiredParameters);
		}
		//===========================================================================================
		private void AddEnumValues(Type enumType, XElement restriction)
		{
			foreach (string name in Enum.GetNames(enumType))
			{
				if (name == "Undefined")
					continue;

				restriction.Add(new XElement(_Namespace + "enumeration",
										new XAttribute("value", name)));
			}
		}
		//===========================================================================================
		private void AddMagickImageMethods(XElement annotation)
		{
			foreach (MethodInfo[] overloads in _MagickNET.GetGroupedMagickImageMethods())
			{
				annotation.AddBeforeSelf(CreateElement(overloads));
			}
		}
		//===========================================================================================
		private void AddMagickImageProperties(XElement annotation)
		{
			foreach (PropertyInfo property in _MagickNET.GetMagickImageProperties())
			{
				annotation.AddBeforeSelf(CreateElement(property));
			}
		}
		//===========================================================================================
		private void AddParameterAttributes(XElement complexType, ParameterInfo[] parameters, string[] requiredParameters)
		{
			foreach (var paramElem in from parameter in parameters
											  let typeName = GetAttributeType(parameter)
											  where typeName != null
											  select new
											  {
												  Name = parameter.Name,
												  TypeName = typeName,
												  IsRequired = requiredParameters.Contains(parameter.Name)
											  })
			{
				XElement attribute = new XElement(_Namespace + "attribute",
												new XAttribute("name", paramElem.Name));

				if (paramElem.IsRequired)
					attribute.Add(new XAttribute("use", "required"));

				attribute.Add(new XAttribute("type", paramElem.TypeName));

				complexType.Add(attribute);
			}
		}
		//===========================================================================================
		private void AddParameterElements(XElement complexType, ParameterInfo[] parameters, string[] requiredParameters)
		{
			XElement sequence = new XElement(_Namespace + "sequence");

			foreach (var paramElem in from parameter in parameters
											  let typeName = GetElementType(parameter)
											  where typeName != null
											  select new
											  {
												  Name = parameter.Name,
												  TypeName = typeName,
												  IsRequired = requiredParameters.Contains(parameter.Name)
											  })
			{
				XElement element = new XElement(_Namespace + "element",
											new XAttribute("name", paramElem.Name));

				if (!paramElem.IsRequired)
					element.Add(new XAttribute("minOccurs", "0"));

				element.Add(new XAttribute("type", paramElem.TypeName));

				sequence.Add(element);
			}

			if (sequence.HasElements)
				complexType.Add(sequence);
		}
		//===========================================================================================
		private object CreateElement(IEnumerable<MethodBase> methods)
		{
			XElement element = new XElement(_Namespace + "element",
										new XAttribute("name", GetName(methods.First())));

			ParameterInfo[] parameters = (from method in methods
													from parameter in method.GetParameters()
													select parameter).ToArray();
			if (parameters.Length > 0)
			{
				if (methods.Count() == 1 && IsTypedElement(parameters))
				{
					element.Add(new XAttribute("type", GetName(parameters[0])));
				}
				else
				{
					XElement complexType = new XElement(_Namespace + "complexType");

					AddArguments(complexType, methods);

					element.Add(complexType);
				}
			}

			return element;
		}
		//===========================================================================================
		private XElement CreateElement(PropertyInfo property)
		{
			string name = GetName(property);

			XElement complexType = new XElement(_Namespace + "complexType");

			string attributeTypeName = GetAttributeType(property);

			if (attributeTypeName != null)
			{
				complexType.Add(new XElement(_Namespace + "attribute",
										new XAttribute("name", "value"),
										new XAttribute("use", "required"),
										new XAttribute("type", attributeTypeName)));
			}
			else
			{
				string elementTypeName = GetElementType(property);

				complexType.Add(new XElement(_Namespace + "sequence",
										new XElement(_Namespace + "element",
											new XAttribute("name", elementTypeName),
											new XAttribute("type", elementTypeName))));
			}

			return new XElement(_Namespace + "element", new XAttribute("name", name), complexType);
		}
		//===========================================================================================
		private XElement CreateEnumElement(Type enumType)
		{
			XElement restriction = new XElement(_Namespace + "restriction", new XAttribute("base", "xs:NMTOKEN"));
			AddEnumValues(enumType, restriction);

			return new XElement(_Namespace + "simpleType",
						new XAttribute("name", enumType.Name),
						restriction);
		}
		//===========================================================================================
		private static void Generate(QuantumDepth depth)
		{
			XsdGenerator generator = new XsdGenerator(depth);
			generator.WriteDocument();
		}
		//===========================================================================================
		private static string GetXsdAttributeType(Type type)
		{
			string typeName = MagickNET.GetTypeName(type);

			if (type.IsEnum)
				return typeName;

			switch (typeName)
			{
				case "Encoding^":
				case "String^":
					return "xs:string";
				case "Percentage":
					return "xs:double";
				case "bool":
					return "xs:boolean";
				case "int":
				case "double":
					return "xs:" + typeName;
				case "Magick::Quantum":
					return "quantum";
				case "MagickColor^":
					return "color";
				case "Coordinate":
				case "Drawable^":
				case "IEnumerable<Coordinate>^":
				case "IEnumerable<Drawable^>^":
				case "IEnumerable<PathArc^>^":
				case "IEnumerable<PathCurveto^>^":
				case "IEnumerable<PathQuadraticCurveto^>^":
				case "IEnumerable<PathBase^>^":
				case "ImageProfile^":
				case "MagickGeometry^":
				case "MagickImage^":
				case "PathArc^":
				case "PathCurveto^":
				case "PathQuadraticCurveto^":
					return null;
				default:
					throw new NotImplementedException(typeName);
			}
		}
		//===========================================================================================
		private static string GetXsdElementType(Type type)
		{
			string typeName = MagickNET.GetTypeName(type);

			switch (typeName)
			{
				case "Coordinate":
					return "coordinate";
				case "Drawable^":
					return "drawable";
				case "IEnumerable<Coordinate>^":
					return "coordinates";
				case "IEnumerable<Drawable^>^":
					return "drawables";
				case "IEnumerable<PathBase^>^":
					return "paths";
				case "IEnumerable<PathArc^>^":
					return "pathArcs";
				case "IEnumerable<PathCurveto^>^":
					return "pathCurvetos";
				case "IEnumerable<PathQuadraticCurveto^>^":
					return "pathQuadraticCurvetos";
				case "ImageProfile^":
					return "profile";
				case "MagickGeometry^":
					return "geometry";
				case "MagickImage^":
					return "read";
				case "PathArc^":
					return "pathArc";
				case "PathCurveto^":
					return "pathCurveto";
				case "PathQuadraticCurveto^":
					return "pathQuadraticCurveto";
				default:
					return null;
			}
		}
		//===========================================================================================
		private static string GetXsdName(string name)
		{
			string newName = name;
			if (name.ToUpperInvariant() == name)
				return name.ToLowerInvariant();

			return char.ToLowerInvariant(name[0]) + name.Substring(1);
		}
		//===========================================================================================
		private bool IsTypedElement(ParameterInfo[] parameters)
		{
			if (parameters.Length > 1)
				return false;

			ParameterInfo parameter = parameters[0];

			if (parameter.ParameterType.IsGenericType && parameter.ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				return true;

			if (parameter.Name == "profile")
				return true;

			return false;
		}
		//===========================================================================================
		private void RemoveComments()
		{
			_Document = XDocument.Load(AppDomain.CurrentDomain.BaseDirectory + @"\..\..\..\Xsd\MagickScript.xsd");
			_Document.DescendantNodes().OfType<XComment>().Remove();
		}
		//===========================================================================================
		private void RemoveUnusedSimpleTypes()
		{
			XElement[] simpleTypes = (from element in _Document.XPathSelectElements("//xs:simpleType", _Namespaces)
											  select element).ToArray();

			foreach (XElement simpleType in simpleTypes)
			{
				string name = (string)simpleType.Attribute("name");

				if (!_Document.XPathSelectElements("//xs:attribute[@type='" + name + "']", _Namespaces).Any())
					simpleType.Remove();
			}
		}
		//===========================================================================================
		private void ReplaceActions(XElement annotation)
		{
			AddMagickImageProperties(annotation);
			AddMagickImageMethods(annotation);

			annotation.Remove();
		}
		//===========================================================================================
		private void ReplaceAnnotations()
		{
			XElement[] annotations = (from element in _Document.XPathSelectElements("//xs:annotation", _Namespaces)
											  select element).ToArray();

			foreach (XElement annotation in annotations)
			{
				string annotationID = annotation.Attribute("id").Value;
				switch (annotationID)
				{
					case "actions":
						ReplaceActions(annotation);
						break;
					case "color":
						ReplaceColor(annotation);
						break;
					case "coordinate":
					case "imageProfile":
					case "pathArc":
					case "pathCurveto":
					case "pathQuadraticCurveto":
						ReplaceWithType(annotation, annotationID);
						break;
					case "drawables":
						ReplaceDrawables(annotation);
						break;
					case "enums":
						ReplaceEnums(annotation);
						break;
					case "geometry":
						ReplaceWithType(annotation, "MagickGeometry");
						break;
					case "paths":
						ReplacePaths(annotation);
						break;
					case "quantum":
						ReplaceQuantum(annotation);
						break;
					default:
						throw new NotImplementedException(annotationID);
				}
			}
		}
		//===========================================================================================
		private void ReplaceEnums(XElement annotation)
		{
			foreach (Type enumType in _MagickNET.Enums)
			{
				annotation.AddBeforeSelf(CreateEnumElement(enumType));
			}

			annotation.Remove();
		}
		//===========================================================================================
		private void ReplaceDrawables(XElement annotation)
		{
			foreach (ConstructorInfo[] constructors in _MagickNET.GetDrawables())
			{
				annotation.AddBeforeSelf(CreateElement(constructors));
			}
			annotation.Remove();
		}
		//===========================================================================================
		private void ReplaceColor(XElement annotation)
		{
			XElement restriction = new XElement(_Namespace + "restriction",
											new XAttribute("base", "xs:string"));

			if (_Depth >= QuantumDepth.Q8)
			{
				restriction.Add(new XElement(_Namespace + "pattern",
										new XAttribute("value", "#([0-9a-fA-F]{3,4})")));

				restriction.Add(new XElement(_Namespace + "pattern",
										new XAttribute("value", "#([0-9a-fA-F]{2}){3,4}")));
			}

			if (_Depth >= QuantumDepth.Q16)
				restriction.Add(new XElement(_Namespace + "pattern",
										new XAttribute("value", "#([0-9a-fA-F]{4}){3,4}")));

			annotation.ReplaceWith(new XElement(_Namespace + "simpleType",
											new XAttribute("name", "color"),
											restriction));
		}
		//===========================================================================================
		private void ReplacePaths(XElement annotation)
		{
			foreach (ConstructorInfo[] constructors in _MagickNET.GetPaths())
			{
				annotation.AddBeforeSelf(CreateElement(constructors));
			}

			annotation.Remove();
		}
		//===========================================================================================
		private void ReplaceQuantum(XElement annotation)
		{
			string typeName;

			switch (_Depth)
			{
				case QuantumDepth.Q16:
					typeName = "xs:unsignedShort";
					break;
				case QuantumDepth.Q8:
					typeName = "xs:unsignedByte";
					break;
				default:
					throw new NotImplementedException();
			}

			annotation.ReplaceWith(new XElement(_Namespace + "simpleType",
											new XAttribute("name", "quantum"),
											new XElement(_Namespace + "restriction", new XAttribute("base", typeName))));
		}
		//===========================================================================================
		private void ReplaceWithType(XElement annotation, string typeName)
		{
			AddArguments(annotation.Parent, _MagickNET.GetConstructors(typeName));

			annotation.Remove();
		}
		//===========================================================================================
		private void Write()
		{
			string outputFile = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\..\Magick.NET\Resources\Release" + _Depth + @"\MagickScript.xsd");
			Console.WriteLine("Creating: " + outputFile);

			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.IndentChars = "\t";

			using (XmlWriter writer = XmlWriter.Create(outputFile, settings))
			{
				_Document.Save(writer);
			}
		}
		//===========================================================================================
		private void WriteDocument()
		{
			RemoveComments();
			ReplaceAnnotations();
			RemoveUnusedSimpleTypes();

			Write();
		}
		//===========================================================================================
		public static void Generate()
		{
			Generate(QuantumDepth.Q8);
			Generate(QuantumDepth.Q16);
		}
		//===========================================================================================
		public static string GetAttributeType(ParameterInfo parameter)
		{
			return GetXsdAttributeType(parameter.ParameterType);
		}
		//===========================================================================================
		public static string GetAttributeType(PropertyInfo property)
		{
			return GetXsdAttributeType(property.PropertyType);
		}
		//===========================================================================================
		public static string GetElementType(ParameterInfo parameter)
		{
			return GetXsdElementType(parameter.ParameterType);
		}
		//===========================================================================================
		public static string GetElementType(PropertyInfo property)
		{
			return GetXsdElementType(property.PropertyType);
		}
		//===========================================================================================
		public static string GetName(MemberInfo member)
		{
			return GetXsdName(MagickNET.GetName(member));
		}
		//===========================================================================================
		public static string GetName(ParameterInfo parameter)
		{
			return GetXsdName(parameter.Name);
		}
		//===========================================================================================
	}
	//==============================================================================================
}
