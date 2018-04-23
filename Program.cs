using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TestConsoleApp
{
    class Program 
    {   
        public static void Main(string[] args)
        {
            Thread thread = new Thread(() => ReadCentralOutQueue("First"));
            thread.Start();
            thread.Join();

            Thread thread2 = new Thread(() => ReadCentralOutQueue("Second"));
            thread2.Start();
            thread2.Join();

            
            
               


            Console.WriteLine("I would read queue {0} here", (1000.53 * 1.15));
            Console.WriteLine("I would read queue {0} here", 1000 * GetVatAmount(true));


            Console.WriteLine("I would read queue {0} here", GetVatAmount(false));

            
        }

        public static void ReadCentralOutQueue(string queueName)
        {
            Console.WriteLine("I would read queue {0} here", queueName);
        }


        public static float GetVatAmount(bool needFull)
        {
            float VatVal = 0;

            if (needFull)
                float.TryParse("1.15", out VatVal);
            else
                float.TryParse("0.15", out VatVal);

            return VatVal;
        }

        public partial class Processor
        {
            public object Process()
            {
                string lineCount = "";

                try
                {
                    string csvData = "";//FileData.ToString();
                    string[] csvLines = csvData.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                    List<PurchaseOrderModel> modelPO = new List<PurchaseOrderModel>();

                    for (int x = 0; x < csvLines.Length; x++)
                    {
                        //Define pattern
                        Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

                        //Separating columns to array
                        string[] lineValues = CSVParser.Split(csvLines[x]);


                        if (lineValues[0].ToString() != "Purchase_ID")
                        {
                            List<PurchaseOrderLineModel> modelPOLines = new List<PurchaseOrderLineModel>();

                            for (int y = 0; y < lineValues.Length; y++)
                            {
                                modelPOLines.Add(new PurchaseOrderLineModel()
                                {
                                    Line = lineValues[y].ToString()
                                });
                            }

                            modelPO.Add(new PurchaseOrderModel()
                            {
                                Lines = modelPOLines
                            });
                        }

                        lineCount += lineValues.Length.ToString() + ";";
                    }

                    if (modelPO.Any(x => x.Lines.Count < 109))
                    {
                        XmlDocument errorDoc = new XmlDocument();
                        XmlElement err = (XmlElement)errorDoc.AppendChild(errorDoc.CreateElement("Errors"));
                        err.AppendChild(errorDoc.CreateElement("Error")).InnerText = "Document lines does not match the quantity needed";

                        return errorDoc.OuterXml;
                    }

                    //Create check list
                    List<string> docNumbers = new List<string>();

                    //Create XML document
                    XmlDocument doc = new XmlDocument();
                    XmlElement el = (XmlElement)doc.AppendChild(doc.CreateElement("Message"));
                    XmlNode purs = el.AppendChild(doc.CreateElement("Purchases"));
                    XmlNode msg = el.AppendChild(doc.CreateElement("Message_UDFs"));

                    //Loop through all the lines in CSV
                    for (int x = 0; x < modelPO.Count; x++)
                    {
                        //Check if PO number was added to check list
                        if (!docNumbers.Contains(modelPO[x].Lines[5].Line.Trim()))
                        {
                            XmlNode purchase = purs.AppendChild(doc.CreateElement("Purchase"));
                            purchase.AppendChild(doc.CreateElement("Purchase_ID")).InnerText = modelPO[x].Lines[0].Line.Trim('"').Trim('\'').Trim();
                            purchase.AppendChild(doc.CreateElement("Purchase_Timestamp")).InnerText = modelPO[x].Lines[1].Line.Trim('"').Trim('\'').Trim();
                            purchase.AppendChild(doc.CreateElement("Purchase_Action")).InnerText = modelPO[x].Lines[2].Line.Trim('"').Trim('\'').Trim();
                            purchase.AppendChild(doc.CreateElement("Tracking_No")).InnerText = modelPO[x].Lines[3].Line.Trim('"').Trim('\'').Trim();
                            purchase.AppendChild(doc.CreateElement("Purchase_Type")).InnerText = modelPO[x].Lines[4].Line.Trim('"').Trim('\'').Trim();
                            purchase.AppendChild(doc.CreateElement("Purchase_Order_No")).InnerText = modelPO[x].Lines[5].Line.Trim('"').Trim('\'').Trim();

                            #region Adding Purchaser Data

                            XmlNode purchaser = purchase.AppendChild(doc.CreateElement("Purchaser"));
                            purchaser.AppendChild(doc.CreateElement("Account_No")).InnerText = modelPO[x].Lines[6].Line.Trim('"').Trim('\'').Trim();
                            purchaser.AppendChild(doc.CreateElement("Name")).InnerText = modelPO[x].Lines[7].Line.Trim('"').Trim('\'').Trim();
                            purchaser.AppendChild(doc.CreateElement("Reg_No")).InnerText = modelPO[x].Lines[8].Line.Trim('"').Trim('\'').Trim();
                            purchaser.AppendChild(doc.CreateElement("Tax_No")).InnerText = modelPO[x].Lines[9].Line.Trim('"').Trim('\'').Trim();

                            XmlNode purchaserAddress = purchaser.AppendChild(doc.CreateElement("Address"));
                            purchaserAddress.AppendChild(doc.CreateElement("Type")).InnerText = modelPO[x].Lines[10].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("Type_Name")).InnerText = modelPO[x].Lines[11].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("Street_No")).InnerText = modelPO[x].Lines[12].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("Street_Name1")).InnerText = modelPO[x].Lines[13].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("Street_Name2")).InnerText = modelPO[x].Lines[14].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("Suburb")).InnerText = modelPO[x].Lines[15].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("City")).InnerText = modelPO[x].Lines[16].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("State")).InnerText = modelPO[x].Lines[17].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("Country_Code")).InnerText = modelPO[x].Lines[18].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("Postal_Code")).InnerText = modelPO[x].Lines[19].Line.Trim('"').Trim('\'').Trim();
                            purchaserAddress.AppendChild(doc.CreateElement("Area_Type")).InnerText = modelPO[x].Lines[20].Line.Trim('"').Trim('\'').Trim();

                            List<string> purchaserIdentityNumbersList = new List<string>();
                            XmlNode purchaserContacts = purchaser.AppendChild(doc.CreateElement("Contacts"));
                            //Loop through PO lines to get the PO's that are the same
                            for (int i = 0; i < modelPO.Count; i++)
                            {
                                if (modelPO[i].Lines[5].Line.Trim() == modelPO[x].Lines[5].Line.Trim())
                                {
                                    if (!purchaserIdentityNumbersList.Contains(modelPO[i].Lines[26].Line.Trim()))
                                    {
                                        XmlNode purchaserContact = purchaserContacts.AppendChild(doc.CreateElement("Contact"));
                                        purchaserContact.AppendChild(doc.CreateElement("Name")).InnerText = modelPO[i].Lines[21].Line.Trim('"').Trim('\'').Trim();
                                        purchaserContact.AppendChild(doc.CreateElement("Designation")).InnerText = modelPO[i].Lines[22].Line.Trim('"').Trim('\'').Trim();
                                        purchaserContact.AppendChild(doc.CreateElement("Telephone")).InnerText = modelPO[i].Lines[23].Line.Trim('"').Trim('\'').Trim();
                                        purchaserContact.AppendChild(doc.CreateElement("Cellphone")).InnerText = modelPO[i].Lines[24].Line.Trim('"').Trim('\'').Trim();
                                        purchaserContact.AppendChild(doc.CreateElement("Email")).InnerText = modelPO[i].Lines[25].Line.Trim('"').Trim('\'').Trim();
                                        purchaserContact.AppendChild(doc.CreateElement("Identity_No")).InnerText = modelPO[i].Lines[26].Line.Trim('"').Trim('\'').Trim();

                                        purchaserIdentityNumbersList.Add(modelPO[i].Lines[26].Line.Trim());
                                    }
                                }
                            }

                            #endregion

                            #region Adding Carrier Data

                            XmlNode carrier = purchase.AppendChild(doc.CreateElement("Carrier"));
                            carrier.AppendChild(doc.CreateElement("Account_No")).InnerText = modelPO[x].Lines[27].Line.Trim('"').Trim('\'').Trim();
                            carrier.AppendChild(doc.CreateElement("Name")).InnerText = modelPO[x].Lines[28].Line.Trim('"').Trim('\'').Trim();
                            carrier.AppendChild(doc.CreateElement("Reg_No")).InnerText = modelPO[x].Lines[29].Line.Trim('"').Trim('\'').Trim();
                            carrier.AppendChild(doc.CreateElement("Tax_No")).InnerText = modelPO[x].Lines[30].Line.Trim('"').Trim('\'').Trim();

                            XmlNode carrierAddress = carrier.AppendChild(doc.CreateElement("Address"));
                            carrierAddress.AppendChild(doc.CreateElement("Type")).InnerText = modelPO[x].Lines[31].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("Type_Name")).InnerText = modelPO[x].Lines[32].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("Street_No")).InnerText = modelPO[x].Lines[33].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("Street_Name1")).InnerText = modelPO[x].Lines[34].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("Street_Name2")).InnerText = modelPO[x].Lines[35].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("Suburb")).InnerText = modelPO[x].Lines[36].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("City")).InnerText = modelPO[x].Lines[37].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("State")).InnerText = modelPO[x].Lines[38].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("Country_Code")).InnerText = modelPO[x].Lines[39].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("Postal_Code")).InnerText = modelPO[x].Lines[40].Line.Trim('"').Trim('\'').Trim();
                            carrierAddress.AppendChild(doc.CreateElement("Area_Type")).InnerText = modelPO[x].Lines[41].Line.Trim('"').Trim('\'').Trim();

                            List<string> carrierIdentityNumbersList = new List<string>();
                            XmlNode carrierContacts = carrier.AppendChild(doc.CreateElement("Contacts"));
                            //Loop through PO lines to get the PO's that are the same
                            for (int i = 0; i < modelPO.Count; i++)
                            {
                                if (modelPO[i].Lines[5].Line.Trim() == modelPO[x].Lines[5].Line.Trim())
                                {
                                    if (!carrierIdentityNumbersList.Contains(modelPO[i].Lines[47].Line.Trim()))
                                    {
                                        XmlNode carrierContact = carrierContacts.AppendChild(doc.CreateElement("Contact"));
                                        carrierContact.AppendChild(doc.CreateElement("Name")).InnerText = modelPO[i].Lines[42].Line.Trim('"').Trim('\'').Trim();
                                        carrierContact.AppendChild(doc.CreateElement("Designation")).InnerText = modelPO[i].Lines[43].Line.Trim('"').Trim('\'').Trim();
                                        carrierContact.AppendChild(doc.CreateElement("Telephone")).InnerText = modelPO[i].Lines[44].Line.Trim('"').Trim('\'').Trim();
                                        carrierContact.AppendChild(doc.CreateElement("Cellphone")).InnerText = modelPO[i].Lines[45].Line.Trim('"').Trim('\'').Trim();
                                        carrierContact.AppendChild(doc.CreateElement("Email")).InnerText = modelPO[i].Lines[46].Line.Trim('"').Trim('\'').Trim();
                                        carrierContact.AppendChild(doc.CreateElement("Identity_No")).InnerText = modelPO[i].Lines[47].Line.Trim('"').Trim('\'').Trim();

                                        carrierIdentityNumbersList.Add(modelPO[i].Lines[47].Line.Trim());
                                    }
                                }
                            }

                            #endregion

                            #region Adding Receiver Data

                            XmlNode receiver = purchase.AppendChild(doc.CreateElement("Receiver"));
                            receiver.AppendChild(doc.CreateElement("Account_No")).InnerText = modelPO[x].Lines[48].Line.Trim('"').Trim('\'').Trim();
                            receiver.AppendChild(doc.CreateElement("Name")).InnerText = modelPO[x].Lines[49].Line.Trim('"').Trim('\'').Trim();
                            receiver.AppendChild(doc.CreateElement("Reg_No")).InnerText = modelPO[x].Lines[50].Line.Trim('"').Trim('\'').Trim();
                            receiver.AppendChild(doc.CreateElement("Tax_No")).InnerText = modelPO[x].Lines[51].Line.Trim('"').Trim('\'').Trim();

                            XmlNode receiverAddress = receiver.AppendChild(doc.CreateElement("Address"));
                            receiverAddress.AppendChild(doc.CreateElement("Type")).InnerText = modelPO[x].Lines[52].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("Type_Name")).InnerText = modelPO[x].Lines[53].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("Street_No")).InnerText = modelPO[x].Lines[54].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("Street_Name1")).InnerText = modelPO[x].Lines[55].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("Street_Name2")).InnerText = modelPO[x].Lines[56].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("Suburb")).InnerText = modelPO[x].Lines[57].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("City")).InnerText = modelPO[x].Lines[58].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("State")).InnerText = modelPO[x].Lines[59].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("Country_Code")).InnerText = modelPO[x].Lines[60].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("Postal_Code")).InnerText = modelPO[x].Lines[61].Line.Trim('"').Trim('\'').Trim();
                            receiverAddress.AppendChild(doc.CreateElement("Area_Type")).InnerText = modelPO[x].Lines[62].Line.Trim('"').Trim('\'').Trim();

                            List<string> receiverIdentityNumbersList = new List<string>();
                            XmlNode receiverContacts = receiver.AppendChild(doc.CreateElement("Contacts"));
                            //Loop through PO lines to get the PO's that are the same
                            for (int i = 0; i < modelPO.Count; i++)
                            {
                                if (modelPO[i].Lines[5].Line.Trim() == modelPO[x].Lines[5].Line.Trim())
                                {
                                    if (!receiverIdentityNumbersList.Contains(modelPO[i].Lines[68].Line.Trim()))
                                    {
                                        XmlNode receiverContact = receiverContacts.AppendChild(doc.CreateElement("Contact"));
                                        receiverContact.AppendChild(doc.CreateElement("Name")).InnerText = modelPO[i].Lines[63].Line.Trim('"').Trim('\'').Trim();
                                        receiverContact.AppendChild(doc.CreateElement("Designation")).InnerText = modelPO[i].Lines[64].Line.Trim('"').Trim('\'').Trim();
                                        receiverContact.AppendChild(doc.CreateElement("Telephone")).InnerText = modelPO[i].Lines[65].Line.Trim('"').Trim('\'').Trim();
                                        receiverContact.AppendChild(doc.CreateElement("Cellphone")).InnerText = modelPO[i].Lines[66].Line.Trim('"').Trim('\'').Trim();
                                        receiverContact.AppendChild(doc.CreateElement("Email")).InnerText = modelPO[i].Lines[67].Line.Trim('"').Trim('\'').Trim();
                                        receiverContact.AppendChild(doc.CreateElement("Identity_No")).InnerText = modelPO[i].Lines[68].Line.Trim('"').Trim('\'').Trim();

                                        receiverIdentityNumbersList.Add(modelPO[i].Lines[68].Line.Trim());
                                    }
                                }
                            }

                            #endregion

                            #region Adding Purchase Line Data

                            List<string> purchaseLineIdsList = new List<string>();
                            XmlNode purchaseLines = purchase.AppendChild(doc.CreateElement("Purchase_Lines"));
                            //Loop through PO lines to get the PO's that are the same
                            for (int i = 0; i < modelPO.Count; i++)
                            {
                                if (modelPO[i].Lines[5].Line.Trim() == modelPO[x].Lines[5].Line.Trim())
                                {
                                    if (!purchaseLineIdsList.Contains(modelPO[i].Lines[69].Line.Trim()))
                                    {
                                        XmlNode purchaseLine = purchaseLines.AppendChild(doc.CreateElement("Purchase_Line"));
                                        purchaseLine.AppendChild(doc.CreateElement("Purchase_Line_ID")).InnerText = modelPO[i].Lines[69].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Purchase_Line_No")).InnerText = modelPO[i].Lines[70].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Purchase_Line_Action")).InnerText = modelPO[i].Lines[71].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Product_Code")).InnerText = modelPO[i].Lines[72].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Inventory_Status")).InnerText = modelPO[i].Lines[73].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Quantity")).InnerText = modelPO[i].Lines[74].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Unit_Price")).InnerText = modelPO[i].Lines[75].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Subtotal")).InnerText = modelPO[i].Lines[76].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Discount")).InnerText = modelPO[i].Lines[77].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Tax")).InnerText = modelPO[i].Lines[78].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLine.AppendChild(doc.CreateElement("Total")).InnerText = modelPO[i].Lines[79].Line.Trim('"').Trim('\'').Trim();

                                        XmlNode purchaseLineUDFs = purchaseLine.AppendChild(doc.CreateElement("Purchase_Line_UDFs"));
                                        XmlNode purchaseLineUDFExtraField = purchaseLineUDFs.AppendChild(doc.CreateElement("Extra_Field"));
                                        purchaseLineUDFExtraField.AppendChild(doc.CreateElement("UDF_Code")).InnerText = modelPO[i].Lines[80].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLineUDFExtraField.AppendChild(doc.CreateElement("UDF_Value")).InnerText = modelPO[i].Lines[81].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLineUDFExtraField.AppendChild(doc.CreateElement("UDF_Description")).InnerText = modelPO[i].Lines[82].Line.Trim('"').Trim('\'').Trim();
                                        purchaseLineUDFExtraField.AppendChild(doc.CreateElement("UDF_Datatype")).InnerText = modelPO[i].Lines[83].Line.Trim('"').Trim('\'').Trim();

                                        purchaseLineIdsList.Add(modelPO[i].Lines[69].Line.Trim());
                                    }
                                }
                            }

                            #endregion

                            #region Adding Purchase Options Data

                            XmlNode purchaseOptions = purchase.AppendChild(doc.CreateElement("Purchase_Options"));
                            purchaseOptions.AppendChild(doc.CreateElement("Delivery_Method")).InnerText = modelPO[x].Lines[84].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("WMS_Company")).InnerText = modelPO[x].Lines[85].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("WMS_Warehouse")).InnerText = modelPO[x].Lines[86].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("Not_Before_Date")).InnerText = modelPO[x].Lines[87].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("Delivery_Date")).InnerText = modelPO[x].Lines[88].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("Allow_Short_Shipment")).InnerText = modelPO[x].Lines[89].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("Allow_Consolidation")).InnerText = modelPO[x].Lines[90].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("Consolidated")).InnerText = modelPO[x].Lines[91].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("Consolidated_ID")).InnerText = modelPO[x].Lines[92].Line.Trim('"').Trim('\'').Trim();
                            purchaseOptions.AppendChild(doc.CreateElement("Special_Instructions")).InnerText = modelPO[x].Lines[93].Line.Trim('"').Trim('\'').Trim();

                            #endregion

                            #region Adding Purchase Values Data

                            XmlNode purchaseValues = purchase.AppendChild(doc.CreateElement("Purchase_Values"));
                            purchaseValues.AppendChild(doc.CreateElement("Subtotal")).InnerText = modelPO[x].Lines[94].Line.Trim('"').Trim('\'').Trim();
                            purchaseValues.AppendChild(doc.CreateElement("Discount")).InnerText = modelPO[x].Lines[95].Line.Trim('"').Trim('\'').Trim();
                            purchaseValues.AppendChild(doc.CreateElement("Freight_Charge")).InnerText = modelPO[x].Lines[96].Line.Trim('"').Trim('\'').Trim();
                            purchaseValues.AppendChild(doc.CreateElement("Tax")).InnerText = modelPO[x].Lines[97].Line.Trim('"').Trim('\'').Trim();
                            purchaseValues.AppendChild(doc.CreateElement("Total")).InnerText = modelPO[x].Lines[98].Line.Trim('"').Trim('\'').Trim();

                            #endregion

                            #region Adding Authorised Recipients Data

                            List<string> authorisedRecipientsList = new List<string>();
                            XmlNode authorisedRecipients = purchase.AppendChild(doc.CreateElement("Authorised_Recipients"));
                            //Loop through PO lines to get the PO's that are the same
                            for (int i = 0; i < modelPO.Count; i++)
                            {
                                if (modelPO[i].Lines[5].Line.Trim() == modelPO[x].Lines[5].Line.Trim())
                                {
                                    if (!authorisedRecipientsList.Contains(modelPO[i].Lines[100].Line.Trim()))
                                    {
                                        XmlNode authorisedRecipient = authorisedRecipients.AppendChild(doc.CreateElement("Authorised_Recipient"));
                                        authorisedRecipient.AppendChild(doc.CreateElement("Name")).InnerText = modelPO[i].Lines[99].Line.Trim('"').Trim('\'').Trim();
                                        authorisedRecipient.AppendChild(doc.CreateElement("Identity_No")).InnerText = modelPO[i].Lines[100].Line.Trim('"').Trim('\'').Trim();

                                        authorisedRecipientsList.Add(modelPO[i].Lines[100].Line.Trim());
                                    }
                                }
                            }

                            #endregion

                            #region Adding Purchase UDFs Data

                            List<string> PurchaseUDFCodes = new List<string>();
                            XmlNode purchaseUDFs = purchase.AppendChild(doc.CreateElement("Purchase_UDFs"));
                            //Loop through PO lines to get the PO's that are the same
                            for (int i = 0; i < modelPO.Count; i++)
                            {
                                if (modelPO[i].Lines[5].Line.Trim() == modelPO[x].Lines[5].Line.Trim())
                                {
                                    if (!PurchaseUDFCodes.Contains(modelPO[i].Lines[101].Line.Trim()))
                                    {
                                        XmlNode purchaseUDFsExtraField = purchaseUDFs.AppendChild(doc.CreateElement("Extra_Field"));
                                        purchaseUDFsExtraField.AppendChild(doc.CreateElement("UDF_Code")).InnerText = modelPO[i].Lines[101].Line.Trim('"').Trim('\'').Trim();
                                        purchaseUDFsExtraField.AppendChild(doc.CreateElement("UDF_Value")).InnerText = modelPO[i].Lines[102].Line.Trim('"').Trim('\'').Trim();
                                        purchaseUDFsExtraField.AppendChild(doc.CreateElement("UDF_Description")).InnerText = modelPO[i].Lines[103].Line.Trim('"').Trim('\'').Trim();
                                        purchaseUDFsExtraField.AppendChild(doc.CreateElement("UDF_Datatype")).InnerText = modelPO[i].Lines[104].Line.Trim('"').Trim('\'').Trim();

                                        PurchaseUDFCodes.Add(modelPO[i].Lines[101].Line.Trim());
                                    }
                                }
                            }

                            #endregion

                            #region Adding Message UDFs Data

                            List<string> messageUDFCodes = new List<string>();

                            for (int i = 0; i < modelPO.Count; i++)
                            {
                                if (modelPO[i].Lines[105].Line.Trim() == modelPO[x].Lines[105].Line.Trim())
                                {
                                    if (!messageUDFCodes.Contains(modelPO[i].Lines[105].Line.Trim()))
                                    {
                                        XmlNode msgExtraField = msg.AppendChild(doc.CreateElement("Extra_Field"));
                                        msgExtraField.AppendChild(doc.CreateElement("UDF_Code")).InnerText = modelPO[x].Lines[105].Line.Trim('"').Trim('\'').Trim();
                                        msgExtraField.AppendChild(doc.CreateElement("UDF_Value")).InnerText = modelPO[x].Lines[106].Line.Trim('"').Trim('\'').Trim();
                                        msgExtraField.AppendChild(doc.CreateElement("UDF_Description")).InnerText = modelPO[x].Lines[107].Line.Trim('"').Trim('\'').Trim();
                                        msgExtraField.AppendChild(doc.CreateElement("UDF_Datatype")).InnerText = modelPO[x].Lines[108].Line.Trim('"').Trim('\'').Trim();

                                        messageUDFCodes.Add(modelPO[x].Lines[105].Line.Trim());
                                    }
                                }
                            }

                            #endregion

                            //Add the PO number to check list
                            docNumbers.Add(modelPO[x].Lines[5].Line.Trim());
                        }
                    }

                    return doc.OuterXml;
                }
                catch (Exception ex)
                {
                    XmlDocument errorDoc = new XmlDocument();
                    XmlElement err = (XmlElement)errorDoc.AppendChild(errorDoc.CreateElement("Errors"));
                    err.AppendChild(errorDoc.CreateElement("Error")).InnerText = ex.Message + " | " + ex.StackTrace;

                    return errorDoc.OuterXml;
                }
            }
        }

        public class PurchaseOrderModel
        {
            public List<PurchaseOrderLineModel> Lines { get; set; }
        }

        public class PurchaseOrderLineModel
        {
            public string Line { get; set; }
        }
    }








}
