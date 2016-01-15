/*
 * *******************************************************
 * Copyright VMware, Inc. 2010-2013.  All Rights Reserved.
 * *******************************************************
 * This is a helper class intended for debug purposes.
 * It reads and writes the OVF Environment of a vApp (containing its Product Properties) to a flat file.
 * It also reads the product properties of the vApps contained VMs and writes them to the console.
 */

using System;
using System.Net;
using com.vmware.vcloud.sdk;
using com.vmware.vcloud.sdk.utility;
using com.vmware.vcloud.api.rest.schema;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace ProductSectionSample
{
    class OVFUtility
    {

        /// <summary>
        /// Reference to the vCloud API Client
        /// </summary>
        private static vCloudClient vcloudClient = null;

        // Certificate to verify 
        private static X509Certificate2 verifyCert = null;
        private static string LocalCertificatePath = string.Empty;

        /// <summary>
        /// Main method, which does Adding, Updating and Deleting a ProductSection
        /// </summary>
        /// <param name="args">args</param>
        /// Args[0] : url of vCD API endpoint
        /// Args[1] : User Name
        /// Args[2] : Password
        /// Args[3] : VDC Name
        /// Args[4] : vApp Name
        /// Args[5] : LocalCertificatePath[optional]
        /// </param>

        static void Main(string[] args)
        {

            try
            {
                // here we have to set the level
                Logger.SourceLevel(Levels.Off);
                if (args.Length < 5)
                    Usage();


                // Client login
                Console.WriteLine("Vcloud Login");
                vcloudClient = new vCloudClient(args[0], com.vmware.vcloud.sdk.constants.Version.V5_5);

                // Performing Certificate Validation 
                if (args.Length == 6)
                {
                    LocalCertificatePath = args[4];
                    Console.WriteLine("	Validating Certificate.");
                    CustomSSLCertificate(LocalCertificatePath);
                }
                else if (args.Length == 5)
                {
                    Console.WriteLine("Ignoring the Certificate Validation using FakeSSLSocketFactory - DO NOT DO THIS IN PRODUCTION");
                    FakeCertificatePolicy();
                }
                else
                {
                    Usage();
                }

                vcloudClient.Login(args[1], args[2]);
                Console.WriteLine("	Login Success\n");

                // Get the Org and VDC

                ReferenceType orgRef = vcloudClient.GetOrgRefByName(vcloudClient.GetOrgName());
                Organization org = Organization.GetOrganizationByReference(vcloudClient, orgRef);

                ReferenceType vdcRef = org.GetVdcRefByName(args[3]);
                Vdc vdc = Vdc.GetVdcByReference(vcloudClient, vdcRef);

                // Get a reference to the vApp

                Dictionary<string, ReferenceType> vapps = vdc.GetVappRefsByName();
                if (!vapps.ContainsKey(args[4]))
                {
                    Console.WriteLine("Error: vApp " + args[4] + " was not found");
                    return;
                }

                ReferenceType vappRef = vapps[args[4]];
                if (vappRef.name != args[4])
                {
                    Console.WriteLine("Error: Unable to obtain valid reference for vApp " + args[4]);
                    return;
                }

                Console.WriteLine("\nInitial Product Properties for vApp " + args[4] + "\n");
                DisplayProductSections(vappRef);


                // FOR DEBUG
                string path = "C:\\tmp\\" + vappRef.name + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_ovf.xml";

                string ovf = Vapp.GetVappByReference(vcloudClient, vappRef).GetOvfAsString();
                //    System.IO.File.WriteAllText(@"C:\tmp\" + vappRef.name + "_ovf.xml", ovf);
                System.IO.File.WriteAllText(@path, ovf);
                Console.WriteLine("Wrote vapp ovf env to " + path);

                Console.WriteLine("\nChecking for VM Properties: ");

                List<VM> vms = Vapp.GetVappByReference(vcloudClient, vappRef).GetChildrenVms();
                foreach (VM vm in vms)
                {
                    Console.WriteLine("Found VM named: " + vm.Resource.name);
                    List<ProductSection_Type> productSections = vm.GetProductSections();
                    foreach (ProductSection_Type ps in productSections)
                    {
                        Object[] items = ps.Items;
                        if (items != null)
                        {
                            Console.WriteLine("ProductProperties for VM " + vm.Resource.name);
                            foreach (Object it in items)
                            {
                                if (it.GetType() == typeof(ProductSection_TypeProperty))
                                {

                                    ProductSection_TypeProperty property = (ProductSection_TypeProperty)it;

                                    // Output and safely handle null values
                                    Console.WriteLine("Property:  ");
                                    Console.WriteLine("    Label: " + string.Format("{0}", property.Label.Value));
                                    Console.WriteLine("    Key: " + string.Format("{0}", property.key));
                                    Console.WriteLine("    Description: " + string.Format("{0}", property.Description));
                                    Console.WriteLine("    Single Value: " + string.Format("{0}", property.value));
                                }
                            }
                        }
                    }

                }
            }

            catch (UnauthorizedAccessException e)
            {
                Logger.Log(TraceLevel.Critical, e.Message);
                Console.WriteLine(e.Message);
            }
            catch (System.IO.IOException e)
            {
                Logger.Log(TraceLevel.Critical, e.Message);
                Console.WriteLine(e.Message);
            }
            catch (VCloudException e)
            {
                Logger.Log(TraceLevel.Critical, e.Message);
                Console.WriteLine(e.Message);
            }
            catch (TimeoutException e)
            {
                Logger.Log(TraceLevel.Critical, e.Message);
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                Logger.Log(TraceLevel.Critical, e.Message);
                Console.WriteLine(e.Message);
            }
        }

        // Usage Example
        // OVFUtility.exe https://us-california-1-3.vchs.vmware.com appstech@websterx.com@ec7274ed-5dfc-43fa-9961-b3611e78aa99 %VCA_PASS% VDC1 CleanVAPP
        public static void Usage()
        {
            Console.WriteLine("OVFUtility vCloudURL user@organization password vdcName vappName LocalCertificatePath[optional]");
            Console.WriteLine("OVFUtility https://vcloud user@System password vdcName vappName");
            Console.WriteLine("OVFUtility https://vcloud user@System password vdcName vappName localcertificatepath");
            Console.WriteLine("Press Enter to Exit");
            Console.Read();
            Environment.Exit(0);
        }
      


        /// <summary>
        /// Display all vApp ProductSection Properties for the specified vApp
        ///  @throws VCloudException
        /// </summary>
        /// <param name="vAppRef">vAppRef</param>
        public static void DisplayProductSections(ReferenceType vAppRef)
        {
            try
            {
                if (vAppRef == null) Console.WriteLine("Error: vAppRef cannot not be null");

                Vapp vapp = Vapp.GetVappByReference(vcloudClient, vAppRef);

                //        Console.WriteLine("\nProduct Properties for vApp " + vapp.Resource.name );
                //       Console.WriteLine("--------------------------------------------------");

                // Get the vApp Product Sections

                List<ProductSection_Type> productTypeList = vapp.GetProductSections();
                if (productTypeList.Count > 0)
                {

                    foreach (ProductSection_Type prodSection in productTypeList)
                    {
                        Console.WriteLine("Property Section:   " + string.Format("{0}", prodSection.Info.Value) +
                            " with id: " + string.Format("{0}", prodSection.instance));

                        Object[] items = prodSection.Items;
                        if (items != null)
                        {
                            foreach (Object it in items)
                            {
                                if (it.GetType() == typeof(ProductSection_TypeProperty))
                                {
                                    ProductSection_TypeProperty property = (ProductSection_TypeProperty)it;

                                    // Output and safely handle null values
                                    Console.WriteLine("Property:  ");
                                    Console.WriteLine("    Label: " + string.Format("{0}", property.Label.Value));
                                    Console.WriteLine("    Key: " + string.Format("{0}", property.key));
                                    Console.WriteLine("    Description: " + string.Format("{0}", property.Description));
                                    Console.WriteLine("    Single Value: " + string.Format("{0}", property.value));

                                    // check since both single and multi-value can be set
                                    if (property.Value != null && property.Value.Length > 0)
                                    {
                                        Console.Write("    Multi-Values: ");
                                        foreach (PropertyConfigurationValue_Type propVal in property.Value)
                                        {
                                            Console.Write(" " + string.Format("{0}", propVal.value));
                                        }
                                    }
                                    Console.WriteLine("\n");
                                }
                            }
                        }
                        else Console.WriteLine("No Product Properties in Section\n");
                    }
                }
                else Console.WriteLine("No Product Sections\n");

            }
            catch (Exception ex)
            {
                throw new VCloudException(ex.Message);
            }
        }



        /// <summary>
        /// Defined a fake certificate policy for accepting the certificate temporarily
        /// </summary>
        public static void FakeCertificatePolicy()
    {
        ServicePointManager.ServerCertificateValidationCallback += new System.Net.
        Security.RemoteCertificateValidationCallback(ValidateServerCertificate);
    }

    /// <summary>
    /// Defined a check whether the certification is trusted or not.
    /// </summary>
    private static bool CustomCertificateValidation(Object sender, X509Certificate
     certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        if (verifyCert.RawData.Length != certificate.GetRawCertData().Length)
        {
            Console.WriteLine("FAILED: Unauthenticated Access.");
            return false;
        }

        for (int i = 0; i < verifyCert.RawData.Length; i++)
            if (verifyCert.RawData[i] != certificate.GetRawCertData()[i])
            {
                Console.WriteLine("FAILED: Unauthenticated Access.");
                return false;
            }
        return true;

    }
        /// <summary>
        ///for testing purpose only, accept any certificate...
        /// </summary>
        private static bool ValidateServerCertificate(object sender, X509Certificate
        certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        /// <summary>
        /// Defined a set certificate policy for accepting the certificate.
        /// </summary>
        public static void CustomSSLCertificate(string LocalCertificatePath)
    {
        verifyCert = new X509Certificate2(LocalCertificatePath);
        ServicePointManager.ServerCertificateValidationCallback += new
          System.Net.Security.RemoteCertificateValidationCallback(CustomCertificateValidation);
    }
}
}

