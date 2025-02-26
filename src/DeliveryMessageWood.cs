﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PapiNet.WoodX
{
    using static XHelpers;

    public enum DeliveryMessageType
    {
        LoadingOrder,
        PackingSpecification,
        DeliveryMessage,
        ShipmentAdvice,
        InitialShipmentAdvice
    }

    public enum DeliveryMessageStatusType
    {
        Cancelled,
        Original,
        Replaced
    }

    public enum PartyType
    {
        PlaceFinalDestination,
        SalesAgent,
        SalesOffice,
        Seller,
        Supplier,
        BillTo,
        Buyer,
        BuyerAgent
    }

    public enum IncotermsType
    {
        EXW,
        DAP,
        DDP,
        CIP,
        DPU,
        FCA,
        CPT
    }

    public class DeliveryMessage(string number, DateTime date)
    {
        const string xsi = "http://www.w3.org/2001/XMLSchema-instance";
        public List<string> Stylesheet { get; set; } = new()
        {
            "ISSsubset_papiNetWoodX_DeliveryMessageWood_V2R31_Build20081207.xls"
        };
        public DeliveryMessageType Type { get; set; } = DeliveryMessageType.DeliveryMessage;
        public DeliveryMessageStatusType Status { get; set; } = DeliveryMessageStatusType.Original;
        public string Number { get; set; } = number;
        public DateTime Date { get; set; } = date;
        public List<Reference> References { get; set; } = [];
        public Party Buyer { get; set; } = new("BuyerParty");
        public Party Supplier { get; set; } = new("SupplierParty");
        public Party Seller { get; set; } = new("OtherParty", "Seller");
        public Party Sender { get; set; } = new("SenderParty");
        public Party Receiver { get; set; } = new("ReceiverParty");
        public Party ShipTo { get; set; } = new("ShipToParty", "PlaceOfDischarge");
        public List<Shipment> Shipments { get; set; } = [];

        public override string ToString()
        {
            return
                new XDocument(
                    Stylesheet.Select(stylesheet => new XProcessingInstruction("xml-stylesheet", "type=\"text/xls\" " + $"href=\"{stylesheet}\"")),
                    new XElement("DeliveryMessageWood",
                        new XAttribute("DeliveryMessageType", Type),
                        new XAttribute("DeliveryMessageStatusType", Status),
                        new XElement("DeliveryMessageWoodHeader",
                            new XElement("DeliveryMessageNumber", Number),
                            new XElement("DeliveryMessageDate", GetXDate(Date)),
                            References.Select(reference => XElement.Parse($"{reference}")),
                            XElement.Parse($"{Buyer}"),
                            XElement.Parse($"{Supplier}"),
                            XElement.Parse($"{Seller}"),
                            XElement.Parse($"{Sender}"),
                            XElement.Parse($"{Receiver}"),
                            new XElement("ShipToInformation",
                                new XElement("ShipToCharacteristics",
                                    XElement.Parse($"{ShipTo}")
                                )
                            )
                        ),
                        Shipments.Select(shipment => XElement.Parse($"{shipment}")),
                        new XElement("DeliveryMessageWoodSummary",
                            new XElement("TotalNumberOfShipments", Shipments.Count)
                        )
                    )
                ).ToString();
        }
    }

    public class Reference(string type, string value, string? assignedBy = null)
    {
        public string Type { get; set; } = type;
        public string Value { get; set; } = value;
        public string? AssignedBy { get; set; } = assignedBy;

        public override string ToString()
        {
            return new XElement("DeliveryMessageReference",
                new XAttribute("DeliveryMessageReferenceType", Type),
                AssignedBy != null ? new XAttribute("AssignedBy", AssignedBy) : null,
                Value
            ).ToString();
        }
    }

    public class Party(string name = "OtherParty", string? type = null, params Identifier[] identifiers)
    {
        public string Name { get; set; } = name;
        public string? Type { get; set; } = type;
        public List<Identifier> Identifiers { get; set; } = identifiers.ToList();
        public NameAddress NameAddress { get; set; } = new();

        public static Party Parse(XElement? element)
        {
            if (element == null)
                return new Party();

            var name = element.Name.LocalName;
            var type = element.Attribute("PartyType")?.Value;

            var identifiers = element.Elements("PartyIdentifier")
                .Select(identifier => new Identifier(
                    identifier.Attribute("PartyIdentifierType")?.Value ?? "Unknown",
                    identifier.Value
                )).ToList();

            var nameAddressElement = element.Element("NameAddress");
            var nameAddress = new NameAddress
            {
                Name1 = nameAddressElement?.Element("Name1")?.Value,
                Address1 = nameAddressElement?.Element("Address1")?.Value,
                Name2 = nameAddressElement?.Element("Name2")?.Value,
                Address2 = nameAddressElement?.Element("Address2")?.Value,
                City = nameAddressElement?.Element("City")?.Value,
                County = nameAddressElement?.Element("County")?.Value,
                PostalCode = nameAddressElement?.Element("PostalCode")?.Value,
                Country = nameAddressElement?.Element("Country")?.Value,
                CountryCode = nameAddressElement?.Element("Country")?.Attribute("CountryCode")?.Value
            };

            return new Party(name, type, identifiers.ToArray()) { NameAddress = nameAddress };
        }

        public override string ToString()
        {
            return new XElement(Name,
                Type != null ? new XAttribute("PartyType", $"{Type}") : null,
                Identifiers.Select(identifier => XElement.Parse($"{identifier}")),
                XElement.Parse($"{NameAddress}")
            ).ToString();
        }
    }

    public class Identifier(string type, string number)
    {
        public string Type { get; set; } = type;
        public string Number { get; set; } = number;

        public override string ToString()
        {
            return new XElement("PartyIdentifier",
                new XAttribute("PartyIdentifierType", $"{Type}"),
                Number
            ).ToString();
        }
    }

    public class NameAddress
    {
        public string? Name1 { get; set; } = null;
        public string? Address1 { get; set; } = null;
        public string? Name2 { get; set; } = null;
        public string? Address2 { get; set; } = null;
        public string? City { get; set; } = null;
        public string? County { get; set; } = null;
        public string? PostalCode { get; set; } = null;
        public string? Country { get; set; } = null;
        public string? CountryCode { get; set; } = null;

        public override string ToString()
        {
            return new XElement("NameAddress",
                Name1 != null ? new XElement("Name1", Name1) : null,
                Address1 != null ? new XElement("Address1", Address1) : null,
                Name2 != null ? new XElement("Name2", Name2) : null,
                Address2 != null ? new XElement("Address2", Address2) : null,
                City != null ? new XElement("City", City) : null,
                County != null ? new XElement("County", County) : null,
                PostalCode != null ? new XElement("PostalCode", PostalCode) : null,
                Country != null ? new XElement("Country",
                    CountryCode != null ? new XAttribute("CountryCode", CountryCode) : null,
                    Country) : null
            ).ToString();
        }
    }

    public class Shipment
    {
        public List<ProductGroup> ProductGroups { get; set; } = [];

        public override string ToString()
        {
            return new XElement("DeliveryMessageShipment",
                ProductGroups.Select(group => XElement.Parse($"{group}"))
            ).ToString();
        }
    }

    public class ProductGroup
    {
        public List<LineItem> LineItems { get; set; } = [];

        public override string ToString()
        {
            return new XElement("DeliveryMessageProductGroup",
                LineItems.Select(item => XElement.Parse($"{item}"))
            ).ToString();
        }
    }

    public class LineItem(string number)
    {
        public string Number { get; set; } = number;
        public List<Product> Products { get; set; } = [];

        public override string ToString()
        {
            return new XElement("DeliveryShipmentLineItem",
                new XElement("DeliveryShipmentLineItemNumber", Number),
                Products.Select(product => XElement.Parse($"{product}"))
            ).ToString();
        }
    }

    public class Product(List<string> descriptions, Classification species, Classification grade, Dimension width, Dimension thickness)
    {
        public List<ProductIdentifier> Identifiers { get; set; } = [];
        public List<string> Descriptions { get; set; } = descriptions;
        public Classification Species { get; set; } = species;
        public Classification Grade { get; set; } = grade;
        public Dimension Width { get; set; } = width;
        public Dimension Thickness { get; set; } = thickness;

        public override string ToString()
        {
            return new XElement("Product",
                Identifiers.Select(identifier => XElement.Parse($"{identifier}")),
                Descriptions.Select(description => new XElement("ProductDescription", description)),
                new XElement("WoodProducts",
                    new XElement("WoodTimbersDimensionalLumberBoards",
                        new XElement("SoftwoodLumber",
                            new XElement("SoftwoodLumberCharacteristics",
                                new XElement("LumberSpecies",
                                    new XAttribute("SpeciesType", Species.Name),
                                    new XElement("SpeciesCode", Species.Code)
                                ),
                                new XElement("LumberGrade",
                                    Grade.Agency != null ? new XAttribute("GradeAgency", Grade.Agency) : null,
                                    new XElement("GradeName", Grade.Name),
                                    new XElement("GradeCode", Grade.Code)
                                ),
                                new XElement("Width", 
                                    new XAttribute("ActualNominal", Width.Type),
                                    new XElement("Value",
                                        new XAttribute("UOM", Width.Unit),
                                        Width.Value
                                    )
                                ),
                                new XElement("Thickness",
                                    new XAttribute("ActualNominal", Thickness.Type),
                                    new XElement("Value",
                                        new XAttribute("UOM", Thickness.Unit),
                                        Thickness.Value
                                    )
                                )
                            )
                        )
                    )
                )
            ).ToString();
        }
    }

    public class ProductIdentifier(string number, string type, string agency)
    {
        public string Number { get; set; } = number;
        public string Type { get; set; } = type;
        public string Agency { get; set; } = agency;

        public override string ToString()
        {
            return new XElement("ProductIdentifier",
                new XAttribute("Agency", Agency),
                new XAttribute("ProductIdentifierType", Type),
                Number
            ).ToString();
        }
    }

    public class Dimension(bool actual, string unit, string value)
    {
        public bool Actual { get; set; } = actual;
        public string Type => Actual ? "Actual" : "Nominal";
        public string Unit { get; set; } = unit;
        public string Value { get; set; } = value;
    }

    public class Classification(string name, string code, string? agency = null)
    {
        public string Name { get; set; } = name;
        public string Code { get; set; } = code;
        public string? Agency { get; set; } = agency;
    }
}
