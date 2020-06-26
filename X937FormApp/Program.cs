using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using CompAnalytics.X9.Document;
using CompAnalytics.X9.Records;
using System.IO;
using System.Data;
//using System.Data.SqlClient;

namespace X937FormApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static X9Document Create(SqlDataReader HDR, SqlDataReader CLHDR, SqlDataReader BHDR, SqlDataReader ChkDR)
        {
            var doc = new X9Document
            {
                Header = new FileHeaderRecord(),
                Trailer = new FileTrailerRecord()
            };

            if (HDR.Read())
            {
                DateTimeOffset createTime = DateTimeOffset.Now;
                DateTimeOffset busDate = DateTimeOffset.Now;
                //string custId = "938476";
                int numRecords = 0;
                int numItems = 0;
                decimal totalAmount = 0;

                numRecords += 2;

                // Configure header
                doc.Header.StandardLevel.SetValue("03");
                doc.Header.TestFileIndicator.SetValue(HDR["FHTestFileIndicator"].ToString());
                doc.Header.ImmediateDestinationRoutingNumber.SetValue(HDR["FHImmediateDestRN"].ToString());
                doc.Header.ImmediateOriginRoutingNumber.SetValue(HDR["FHImmediateOriginRN"].ToString());
                doc.Header.FileCreationDate.SetValue(createTime); // The same DateTimeOffset can be passed into a date & time
                doc.Header.FileCreationTime.SetValue(createTime); // field separately, and the appropriate values will be set
                doc.Header.ResendIndicator.SetValue(HDR["FHResendIndicator"].ToString());
                doc.Header.ImmediateDestinationName.SetValue(HDR["FHImmediateDestName"].ToString());
                doc.Header.ImmediateOriginName.SetValue(HDR["FHImmediateOriginName"].ToString());
                doc.Header.FileIdModifier.SetValue(1);
                doc.Header.CountryCode.SetValue(HDR["FHCountryCode"].ToString());
                doc.Header.UserField.SetValue(HDR["FHUserData"].ToString());
                //doc.Header.CompanionDocumentVersionIndicator.SetValue(1);

                // Create a single deposit
                var dep = new X9Deposit
                {
                    CashLetterHeader = new CashLetterHeaderRecord(),
                    CashLetterTrailer = new CashLetterTrailerRecord()
                };
                numRecords += 2;
                if (CLHDR.Read())
                {
                    dep.CashLetterHeader.CollectionTypeIndicator.SetValue(CLHDR["CHCollectionTypeIndicator"].ToString());
                    dep.CashLetterHeader.DestinationRoutingNumber.SetValue(CLHDR["CHDestRN"].ToString());
                    dep.CashLetterHeader.UniqueCustomerIdentifier.SetValue(CLHDR["CHECEInstitutionRN"].ToString()); //ECE InstRoutingNumber
                    dep.CashLetterHeader.CashLetterBusinessDate.SetValue(busDate);
                    dep.CashLetterHeader.CashLetterCreationDate.SetValue(createTime);
                    dep.CashLetterHeader.CashLetterCreationTime.SetValue(createTime);
                    dep.CashLetterHeader.CashLetterRecordTypeIndicator.SetValue(CLHDR["CHRecTypeIndicator"].ToString());
                    dep.CashLetterHeader.CashLetterDocumentationTypeIndicator.SetValue(CLHDR["CHDocTypeIndicator"].ToString());
                    dep.CashLetterHeader.CashLetterId.SetValue(CLHDR["CHCashLetterID"].ToString());
                    dep.CashLetterHeader.OriginatorContactName.SetValue(CLHDR["CHOriginName"].ToString());
                    dep.CashLetterHeader.PhoneNumberOrUlid.SetValue(CLHDR["CHOriginPhone"].ToString());
                    dep.CashLetterHeader.WorkType.SetValue(CLHDR["CHFedWorkType"].ToString());
                    dep.CashLetterHeader.ReturnIndicator.SetValue("");
                    dep.CashLetterHeader.UserField.SetValue(CLHDR["CHUserData"].ToString());
                }
                // Create a single bundle
                int bundleIdx = 0;
                decimal depositTotalAmount = 0;
                int depositNumItems = 0;
                int depositNumImages = 0;

                var bundle = new X9Bundle
                {
                    Header = new BundleHeaderRecord(),
                    Trailer = new BundleTrailerRecord()
                };
                numRecords += 2;
                if (BHDR.Read())
                {
                    bundle.Header.CollectionTypeIndicator.SetValue(BHDR["BHCollectionTypeIndicator"].ToString());
                    bundle.Header.DestinationRoutingNumber.SetValue(BHDR["BHBundleDestRN"].ToString());
                    bundle.Header.UniqueCustomerIdentifier.SetValue(BHDR["BHBundleECEInstitutionRN"].ToString());
                    bundle.Header.BundleBusinessDate.SetValue(busDate);
                    bundle.Header.BundleCreationDate.SetValue(createTime);
                    bundle.Header.BundleId.SetValue(BHDR["BHBundleID"].ToString());
                    bundle.Header.BundleSequenceNumber.SetValue("1");
                    bundle.Header.CycleNumber.SetValue(BHDR["BHCycleNumber"].ToString());
                    bundle.Header.UserField.SetValue(BHDR["BHUserData"].ToString());
                }
                // Create a few checks based on the ones passed in
                int bundleNumImages = 0;
                decimal bundleTotalAmount = 0;

                //foreach (CheckInfo  in DR)
                while (ChkDR.Read())
                {
                    var depItem = new X9DepositItem()
                    {
                        CheckDetail = new CheckDetailRecord()
                    };
                    numRecords += 1;
                    var ChkAmt = Convert.ToDecimal(ChkDR["ItemAmount"].ToString());

                    depItem.CheckDetail.AuxiliaryOnUs.SetValue(ChkDR["AuxOnUs"].ToString());
                    depItem.CheckDetail.ExternalProcessingCode.SetValue(ChkDR["ExtProcessingCode"].ToString());
                    depItem.CheckDetail.PayorBankRoutingNumber.SetValue(ChkDR["PayerBankRN"].ToString());
                    depItem.CheckDetail.MICROnUs.SetValue(ChkDR["OnUsAcctNumber"].ToString());
                    depItem.CheckDetail.Amount.SetValue(ChkDR["ItemAmount"]);
                    depItem.CheckDetail.EceInstitutionItemSequenceNumber.SetValue(ChkDR["ECEInstItemSeqNum"].ToString());
                    depItem.CheckDetail.DocumentationTypeIndicator.SetValue("G");
                    //depItem.CheckDetail.ReturnAcceptanceIndicator.SetValue(ChkDR[""].ToString());
                    //depItem.CheckDetail.MicrValidIndicator.SetValue(ChkDR[""].ToString());
                    depItem.CheckDetail.BofdIndicator.SetValue(ChkDR["BOFDConversionIndicator"].ToString());
                    //depItem.CheckDetail.CheckDetailRecordAddendumCount.SetValue(ChkDR[""].ToString());
                    depItem.CheckDetail.CorrectionIndicator.SetValue(ChkDR["CorrectionIndicator"].ToString());
                    //depItem.CheckDetail.ArchiveTypeIndicator.SetValue(ChkDR[""].ToString());

                    // Add images for the check

                    // Front first
                    var frontImageRecord = new X9DepositItemImage()
                    {
                        ImageViewDetail = new ImageViewDetailRecord(),
                        ImageViewData = new ImageViewDataRecord()
                    };
                    numRecords += 2;
                    byte[] frontImageBytes = (byte[])ChkDR["Side0Image"];

                    frontImageRecord.ImageViewDetail.ImageIndicator.SetValue(ChkDR["ImageIndicator"].ToString());
                    frontImageRecord.ImageViewDetail.ImageCreatorRoutingNumber.SetValue(BHDR["BHBundleECEInstitutionRN"].ToString());
                    frontImageRecord.ImageViewDetail.ImageCreatorDate.SetValue(busDate);
                    frontImageRecord.ImageViewDetail.ImageViewFormatIndicator.SetValue("00");
                    frontImageRecord.ImageViewDetail.ImageCompressionAlgorithmIndicator.SetValue("00");
                    frontImageRecord.ImageViewDetail.ImageViewDataSize.SetValue(frontImageBytes.Length);
                    frontImageRecord.ImageViewDetail.ViewSideIndicator.SetValue(false); // front
                    frontImageRecord.ImageViewDetail.ViewDescriptor.SetValue("00");
                    frontImageRecord.ImageViewDetail.DigitalSignatureIndicator.SetValue("0");
                    //frontImageRecord.ImageViewDetail.DigitalSignatureMethod.SetValue(ChkDR[""].ToString());
                    //frontImageRecord.ImageViewDetail.SecurityKeySize.SetValue(ChkDR[""].ToString());
                    //frontImageRecord.ImageViewDetail.StartOfProtectedData.SetValue(ChkDR[""].ToString());
                    //frontImageRecord.ImageViewDetail.LengthOfProtectedData.SetValue(ChkDR[""].ToString());
                    //frontImageRecord.ImageViewDetail.ImageRecreateIndicator.SetValue("");

                    frontImageRecord.ImageViewData.EceInstitutionRoutingNumber.SetValue(BHDR["BHBundleECEInstitutionRN"].ToString());
                    frontImageRecord.ImageViewData.BundleBusinessDate.SetValue(busDate);
                    frontImageRecord.ImageViewData.CycleNumber.SetValue("2");
                    frontImageRecord.ImageViewData.EceInstitutionItemSequenceNumber.SetValue(BHDR["BHCycleNumber"].ToString());
                    //frontImageRecord.ImageViewData.SecurityOriginatorName.SetValue(ChkDR[""].ToString());
                    //frontImageRecord.ImageViewData.SecurityAuthenticatorName.SetValue(ChkDR[""].ToString());
                    //frontImageRecord.ImageViewData.SecurityKeyName.SetValue(ChkDR[""].ToString());
                    frontImageRecord.ImageViewData.ClippingOrigin.SetValue("0");
                    //frontImageRecord.ImageViewData.ClippingCoordinateH1.SetValue("");
                    //frontImageRecord.ImageViewData.ClippingCoordinateH2.SetValue("");
                    //frontImageRecord.ImageViewData.ClippingCoordinateV1.SetValue("");
                    //frontImageRecord.ImageViewData.ClippingCoordinateV2.SetValue("");
                    frontImageRecord.ImageViewData.LengthOfImageReferenceKey.SetValue("0000");
                    frontImageRecord.ImageViewData.LengthOfDigitalSignature.SetValue("00000");
                    frontImageRecord.ImageViewData.LengthOfImageData.SetValue(frontImageBytes.Length);
                    frontImageRecord.ImageViewData.ImageData.SetImageBytes(frontImageBytes);

                    depItem.Images.Add(frontImageRecord);
                    bundleNumImages++;

                    // Back
                    var backImageRecord = new X9DepositItemImage()
                    {
                        ImageViewDetail = new ImageViewDetailRecord(),
                        ImageViewData = new ImageViewDataRecord()
                    };
                    numRecords += 2;
                    byte[] backImageBytes = (byte[])ChkDR["Side1Image"];

                    backImageRecord.ImageViewDetail.ImageIndicator.SetValue(ChkDR["ImageIndicator"].ToString());
                    backImageRecord.ImageViewDetail.ImageCreatorRoutingNumber.SetValue(BHDR["BHBundleECEInstitutionRN"].ToString());
                    backImageRecord.ImageViewDetail.ImageCreatorDate.SetValue(busDate);
                    backImageRecord.ImageViewDetail.ImageViewFormatIndicator.SetValue("00");
                    backImageRecord.ImageViewDetail.ImageCompressionAlgorithmIndicator.SetValue("00");
                    backImageRecord.ImageViewDetail.ImageViewDataSize.SetValue(frontImageBytes.Length);
                    backImageRecord.ImageViewDetail.ViewSideIndicator.SetValue(false); // back
                    backImageRecord.ImageViewDetail.ViewDescriptor.SetValue("00");
                    backImageRecord.ImageViewDetail.DigitalSignatureIndicator.SetValue("0");
                    //backImageRecord.ImageViewDetail.DigitalSignatureMethod.SetValue(ChkDR[""].ToString());
                    //backImageRecord.ImageViewDetail.SecurityKeySize.SetValue(ChkDR[""].ToString());
                    //backImageRecord.ImageViewDetail.StartOfProtectedData.SetValue(ChkDR[""].ToString());
                    //backImageRecord.ImageViewDetail.LengthOfProtectedData.SetValue(ChkDR[""].ToString());
                    //backImageRecord.ImageViewDetail.ImageRecreateIndicator.SetValue("");

                    backImageRecord.ImageViewData.EceInstitutionRoutingNumber.SetValue(BHDR["BHBundleECEInstitutionRN"].ToString());
                    backImageRecord.ImageViewData.BundleBusinessDate.SetValue(busDate);
                    backImageRecord.ImageViewData.CycleNumber.SetValue("2");
                    backImageRecord.ImageViewData.EceInstitutionItemSequenceNumber.SetValue(BHDR["BHCycleNumber"].ToString());
                    //backImageRecord.ImageViewData.SecurityOriginatorName.SetValue(ChkDR[""].ToString());
                    //backImageRecord.ImageViewData.SecurityAuthenticatorName.SetValue(ChkDR[""].ToString());
                    //backImageRecord.ImageViewData.SecurityKeyName.SetValue(ChkDR[""].ToString());
                    backImageRecord.ImageViewData.ClippingOrigin.SetValue("0");
                    //backImageRecord.ImageViewData.ClippingCoordinateH1.SetValue("");
                    //backImageRecord.ImageViewData.ClippingCoordinateH2.SetValue("");
                    //backImageRecord.ImageViewData.ClippingCoordinateV1.SetValue("");
                    //backImageRecord.ImageViewData.ClippingCoordinateV2.SetValue("");
                    backImageRecord.ImageViewData.LengthOfImageReferenceKey.SetValue("0000");
                    backImageRecord.ImageViewData.LengthOfDigitalSignature.SetValue("00000");
                    backImageRecord.ImageViewData.ImageData.SetImageBytes(backImageBytes);
                    backImageRecord.ImageViewData.LengthOfImageData.SetValue(backImageBytes.Length);
                    depItem.Images.Add(backImageRecord);
                    bundleNumImages++;

                    bundle.DepositItems.Add(depItem);
                    bundleTotalAmount += ChkAmt;
                }

                // Configure bundle trailer
                bundle.Trailer.ItemsWithinBundleCount.SetValue(bundle.DepositItems.Count);
                bundle.Trailer.ImagesWithinBundleCount.SetValue(bundleNumImages);
                bundle.Trailer.MicrValidTotalAmount.SetValue(bundleTotalAmount);
                bundle.Trailer.UserField.SetValue(BHDR["BCUserData"].ToString());

                dep.Bundles.Add(bundle);
                bundleIdx++;
                depositTotalAmount += bundleTotalAmount;
                depositNumItems += bundle.DepositItems.Count;
                depositNumImages += bundleNumImages;

                // Configure cash letter trailer
                dep.CashLetterTrailer.BundleCount.SetValue(bundleIdx);
                dep.CashLetterTrailer.ImagesWithinCashLetterCount.SetValue(depositNumImages);
                dep.CashLetterTrailer.ItemsWithinCashLetterCount.SetValue(depositNumItems);
                dep.CashLetterTrailer.CashLetterTotalAmount.SetValue(depositTotalAmount);
                dep.CashLetterTrailer.EceInstitutionName.SetValue(CLHDR["ECECEInstitutionName"].ToString());
                dep.CashLetterTrailer.SettlementDate.SetValue(busDate);

                numItems += depositNumItems;
                totalAmount += depositTotalAmount;

                doc.Deposits.Add(dep);

                // Configure file trailer
                doc.Trailer.CashLetterCount.SetValue(doc.Deposits.Count);
                doc.Trailer.TotalRecordCount.SetValue(numRecords);
                doc.Trailer.TotalFileAmount.SetValue(totalAmount);
                doc.Trailer.TotalItemCount.SetValue(numItems);
                doc.Trailer.ImmediateOriginContactName.SetValue(HDR["FCImmediateOrigContactName"].ToString());
                doc.Trailer.ImmediateOriginContactPhone.SetValue(HDR["FCImmediateOrigContactPhone"].ToString());
            }
            return doc;
        }
        public static SqlDataReader GetHeaderData(string procName, int ID, SqlConnection CN)
        {
            //Get SQL Recs from DB
            SqlCommand CMD = new SqlCommand(procName, CN);
            CMD.CommandType = CommandType.StoredProcedure;
            CMD.Parameters.Add("@ID", SqlDbType.Int).Value = ID;

            return CMD.ExecuteReader();
        }
        public static SqlDataReader GetCheckData(string procName, int ID, int Status, SqlConnection CN)
        {
            //Get SQL Recs from DB
            SqlCommand CMD = new SqlCommand(procName, CN);
            CMD.CommandType = CommandType.StoredProcedure;
            CMD.Parameters.Add("@ID", SqlDbType.Int).Value = ID;
            CMD.Parameters.Add("@Status", SqlDbType.TinyInt).Value = Status;

            return CMD.ExecuteReader();
        }
        public static SqlConnection ConnectSQL(string DSN)
        {
            SqlConnection CN;

            CN = new SqlConnection(DSN);
            CN.Open();
            return CN;
        }
    }

    public class CheckInfo
    {
    }
}
