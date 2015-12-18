
/*
 * *******************************************************
 * Copyright VMware, Inc. 2010-2013.  All Rights Reserved.
 * *******************************************************
 * This Sample demonstrates CRUD operations of Product Properties in the ProductSections of a vApp.
 *
 * Product properties are displayed on the vCD console under the Custom Properties tab of the vApp.
 * Product Properties can be set and read using the vCD API and also read from inside the guest OS
 * using the vmtools command
 * vmtoolsd --cmd "info-get guestinfo.ovfEnv"
 *
 * Product Properties are persisted across vApp restarts.
 * NOTE: OVF Environment Properties should be modified with the vApp in the POWERED OFF state.
 *       vmtoolsd will not have access to modifed properties newly until the vApp is started / restarted.
 *
 * Properties can be grouped into Product Sections.
 * In this example, a new property is set in the default ProductSection 
 * The ProductSection is created if it does not exist.
 * The new property is then updated in a subsequent call to set its timestamp value to the current time.
 * 
 * A ProductSection list can contain more than one ProductSection
 * A ProductSection in the ProductSectionList without a unique id or class is
 * considered the default Product Section. Only one default Product Section can exist in the list.
 * Additional ProductSections must have a unique instance id or class name set.
 *
 * ProductSectionSample Documentation Link 
 * http://pubs.vmware.com/vcd-51/index.jsp?topic=%2Fcom.vmware.vcloud.api.doc_51%2FGUID-E13A5613-8A41-46E3-889B-8E1EAF10ABBE.html
 *
 *
 * Sample Limitations: 
 * Only demonstrates manipulation of Properties of type String.
 * Only a single value can be set for each property
 * Properties are manipulated in only a single (default) Product Section in the ProductSectionList
 * Users can change the value of PRODUCT_SECTION_ID to manipulate non default Product Sections.
 *
 */

using System;
using System.Net;
using System.Linq;
using com.vmware.vcloud.sdk;
using com.vmware.vcloud.sdk.utility;
using com.vmware.vcloud.api.rest.schema;
using com.vmware.vcloud.sdk.constants;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;


namespace Com.Vmware.Vcloud.Sdk.Samples
{

    // Usage Example
    // ProductSectionSample.exe https://us-california-1-3.vchs.vmware.com appstech@myco.com@ec7274ed-5dfc-43fa-9961-b3611e78aa99 %VCA_PASS% VDC1 vnew-VM-9c6-VApp

    public class ProductSectionSample
    {
        /// <summary>
        /// Reference to the vCloud API Client
        /// </summary>
        private static vCloudClient vcloudClient = null;

        /// <summary>
        /// Names for the New Property Section and New Property
        /// </summary>
        public const string PRODUCT_SECTION_ID = "";         // Default ProductSection id=""
                                                             // Change value to experiement with >1 product Section
        public const string PROPERTY_LABEL = "newStampLabel";
        public const string PROPERTY_KEY = "newStampKey";

        // Certificate to verify 
        private static X509Certificate2 verifyCert = null;
        private static string LocalCertificatePath = string.Empty;

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

                // Get the vApp Product Section List and output the Properties in each Product Section

                List<ProductSection_Type> productTypeList = vapp.GetProductSections();
                if (productTypeList.Count > 0)
                {

                    foreach (ProductSection_Type prodSection in productTypeList)
                    {
                        Console.WriteLine("Property Section:   " + string.Format("{0}", prodSection.Info.Value) +
                            " with id: " + string.Format("{0}", prodSection.instance));

                        Object[] items = prodSection.Items;
                        if (items != null) { 
                           foreach (Object it in items)
                           {
                               if (it.GetType() == typeof(ProductSection_TypeProperty))
                               {
                                   ProductSection_TypeProperty property = (ProductSection_TypeProperty)it;

                                    // Output and safely handle null values
                                    Console.WriteLine("Property:  ");
                                    Console.WriteLine("    Label: " + string.Format("{0}", property.Label.Value));
                                    Console.WriteLine("    Key: " + string.Format("{0}", property.key));
                                    Console.WriteLine("    Description: " + string.Format("{0}",property.Description));
                                    Console.WriteLine("    Single Value: " + string.Format("{0}", property.value));

                                    // check since both single and multi-value can be set
                                    if (property.Value != null && property.Value.Length > 0)
                                    {
                                        Console.Write("    Multi-Values: ");
                                        foreach(PropertyConfigurationValue_Type propVal in property.Value)
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
        /// Add a custom Property to a vApp ProductSection 
        ///  @throws VCloudException
        /// </summary>
        /// <param name="vAppRef">vAppRef</param>
        /// <param name="productSectionName">productSectionName</param>
        /// <param name="newLabel">newLabel</param>
        /// <param name="newKey">newKey</param>
        /// <param name="newValue">newValue</param>
        public static void AddProductProperty(ReferenceType vAppRef, string productSectionId, string newLabel, string newKey, string newValue)
        {

            try
            {

                if (vAppRef == null) Console.WriteLine("Error: vAppRef cannot not be null");

                if (productSectionId == null) productSectionId = "";  // set to default product section id

                Vapp vapp = Vapp.GetVappByReference(vcloudClient, vAppRef);
              
                ProductSection_Type prodSection = null;
                ProductSection_TypeProperty newProperty = null;
                Object[] items = null;
                bool keyFound = false;

                // Get the vApp ProductSectionList
                List<ProductSection_Type> productTypeList = vapp.GetProductSections();

          
                // Get the ProductSection
                if (productTypeList.Count != 0)
                {
                    // Search for the  ProductSection
                    foreach (ProductSection_Type section in productTypeList)
                    {
                        if (section.instance.Equals(productSectionId))
                        {
                            prodSection = section;
                            break;
                        }
                    }
                }



                // Create new vApp ProductSection if one was not found.
                if (prodSection == null)
                {
                    prodSection = new ProductSection_Type();
                    Msg_Type sectionInfo = new Msg_Type();
                    prodSection.Info = sectionInfo;
                    prodSection.instance = productSectionId;
                    productTypeList.Add(prodSection);
                  
                }

                // Add items array to existing or new ProductSection if empty
                if (prodSection.Items == null)
                { 
                    // Add an items Array to a new Section
                    items = new Object[0];
                    prodSection.Items = items;
                }
                
               else {

                    // Update the property if it already exists
                    
                    for (int idx=0; idx<prodSection.Items.Count(); idx++) 
                    {
                        // Check only items of type Property 
                        if (prodSection.Items[idx].GetType() == typeof(ProductSection_TypeProperty))
                        {
                            ProductSection_TypeProperty property = (ProductSection_TypeProperty)prodSection.Items[idx];

                            // update property if found
                            if (property.key.Equals(newKey)) { 
                              
                                property.value = newValue; // set as an attribute if a single property value

                             // fyi, as shown below set property values in an array if multiple values per property
                             
                             // property.Value = new PropertyConfigurationValue_Type[1];
                             // PropertyConfigurationValue_Type newValueProp = new PropertyConfigurationValue_Type();
                             // newValueProp.value = newValue;
                             // property.Value[0] = newValueProp;

                                keyFound = true;
                                break;
                           }
                        }
                    }   
                }

                // Create a new property if the property does not exist
                if (keyFound == false)
                {
                    // Create a new Property and set value
                    newProperty = new ProductSection_TypeProperty();
                    // Set Label
                    Msg_Type label = new Msg_Type();
                    label.Value = newLabel;
                    newProperty.Label = label;

                    // Set Property Value

                    newProperty.value = newValue; // set as an attribute if a single property value

                    // fyi, as shown below set property values in an array if multiple values per property

                    // newProperty.Value = new PropertyConfigurationValue_Type[1];
                    // PropertyConfigurationValue_Type newValueProp = new PropertyConfigurationValue_Type();
                    // newValueProp.value = newValue;
                    // newProperty.Value[0] = newValueProp;

                    newProperty.userConfigurable = true;
                    newProperty.key = newKey;
                    newProperty.type = "string";
                  
                    // Resize Items Array to hold the new entry
                    Array.Resize(ref items, items.Length + 1);

                    // Add it to the end
                    items[items.Length - 1] = newProperty;
                    prodSection.Items = items;
                }

                // update the existing or new property
                vapp.UpdateProductSections(productTypeList).WaitForTask(0);

            }
            catch (Exception ex)
            {
                throw new VCloudException(ex.Message);
            }
        }




        /// <summary>
        /// Deletes a Product Property from a vApp ProductSection 
        ///  @throws VCloudException
        /// </summary>
        /// <param name="vappRef">vappRef</param>
        /// <param name="productSectionName">productSectionName</param>
        /// <param name="key">key</param>
        /// <returns>Returns true if property removed otherwise false</returns>
        public static bool DeleteProductProperty(ReferenceType vAppRef, string productSectionId, string key)
        {
            try
            {
                if (vAppRef == null)
                {
                    Console.WriteLine("Error: vAppRef cannot not be null");
                    return false;
                }

                if (productSectionId == null) productSectionId = "";  // set to default product section id


                Vapp vapp = Vapp.GetVappByReference(vcloudClient, vAppRef);

                // Get the vApp ProductSectionList

                List<ProductSection_Type> productTypeList = vapp.GetProductSections();
                if (productTypeList.Count != 0)
                {
                    // Search for the ProductSection
                    foreach (ProductSection_Type section in productTypeList)
                    {
                        if (section.instance.Equals(productSectionId))
                        {
                            // Search for the named Property
                            for (int idx = 0; idx < section.Items.Count(); idx++)
                            {
                                if (section.Items[idx].GetType() == typeof(ProductSection_TypeProperty))
                                {
                                    ProductSection_TypeProperty property = (ProductSection_TypeProperty)section.Items[idx];
                                    if (property.key.Equals(key))
                                    {
                                        // Remove the property
                                        section.Items = section.Items.Where((val, off) => off != idx).ToArray();

                                        // Update the ProductSection
                                        vapp.UpdateProductSections(productTypeList).WaitForTask(0);
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
                return false;

            }

            catch (Exception ex)
            {
                throw new VCloudException(ex.Message);
            }
        }

        /// <summary>
        /// Delete a ProductSection 
        ///  @throws VCloudException
        /// </summary>
        /// <param name="vappRef">vappRef</param>
        /// <param name="sectionName">productSectionName</param>
        /// <returns>Returns true if property removed otherwise false</returns>
        public static bool DeleteProductSection(ReferenceType vAppRef, string productSectionId)
        {

            if (vAppRef == null ) return false;

            if (productSectionId == null) productSectionId = "";  // set to default product section id


            Vapp vapp = Vapp.GetVappByReference(vcloudClient, vAppRef);

            List<ProductSection_Type> productTypeList = vapp.GetProductSections();
            if (productTypeList == null) return false;
     
            try
            {
                foreach (ProductSection_Type section in productTypeList)
                {
                    if (section.instance.Equals(productSectionId))
                    {
                        // Remove the section
                        productTypeList.Remove(section);
                        vapp.UpdateProductSections(productTypeList).WaitForTask(0);
                        return true;
                    }
                }
                Console.WriteLine("Warning: ProductSection: " + productSectionId  + " not Found");
                return false;
            }

            catch (Exception ex)
            {
                throw new VCloudException(ex.Message);
            }
        }



        public static void Usage()
        {
            Console.WriteLine("ProductSectionSample vCloudURL user@organization password vdcName vappName LocalCertificatePath[optional]");
            Console.WriteLine("ProductSectionSample https://vcloud user@System password vdcName vappName");
            Console.WriteLine("ProductSectionSample https://vcloud user@System password vdcName vappName localcertificatepath");
            Console.WriteLine("Press Enter to Exit");
            Console.Read();
            Environment.Exit(0);
        }
        /// <summary>
        /// Main method, which does Adding, Updating and Deleting a ProductSection
        /// </summary>
        /// <param name="args">args</param>
        /// Args[0] : VersionUrl
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

   
                // Check the vApp status

                ReferenceType vappRef = vapps[args[4]];
                Vapp vapp = Vapp.GetVappByReference(vcloudClient, vappRef);
                if (!vapp.GetVappStatus().Equals(VappStatus.POWERED_OFF)) 
                    Console.WriteLine("\nWARNING: the vApp must be powered off to update OVF properties.\n");


                // Display the starting/existing Custom Properties for the Vapp
                Console.WriteLine("\nInitial Product Properties for vApp " + args[4] + "\n");
                DisplayProductSections(vappRef);  


                // Add a custom property to the specified vApp, update it if it exists
                Console.WriteLine("Adding a new vApp Custom Property...\n");
                DateTime localDate = DateTime.Now;
                AddProductProperty(vappRef, PRODUCT_SECTION_ID, PROPERTY_LABEL, PROPERTY_KEY, localDate.ToString());

                // Display the updated Custom Properties for the Vapp
                Console.WriteLine("\nNew Product Properties for vApp " + args[4] + "\n");
                DisplayProductSections(vappRef);

                // update a custom property to the specified vApp, update if it exists
                Console.WriteLine("Updating the vApp Custom Property timestamp value...\n");
                DateTime localDateNew = DateTime.Now;
                AddProductProperty(vappRef, PRODUCT_SECTION_ID, PROPERTY_LABEL, PROPERTY_KEY, localDateNew.ToString());

                // Display the updated Custom Properties for the Vapp
                Console.WriteLine("\nUpdated Product Properties for vApp " + args[4] + "\n");
                DisplayProductSections(vappRef);


                // Note: Delete actions below are commented out to enable a user to
                // read the new property from the guestos using vmtoolsd

                // Delete the property Exapmle

                // Console.WriteLine("\nDeleting the new Property...\n");
                // DeleteProductProperty(vappRef, PRODUCT_SECTION_ID, PROPERTY_KEY);
                // Console.WriteLine("\nProduct Properties for vApp " + args[4] + "\n");
                // DisplayProductSections(vappRef);

                // Delete a PropertySection Example

                // Note: Deleting the default PropertySection may result in also deleting
                //       Properties stored by the VMs.
                // Console.WriteLine("\nDeleting the new ProductSection...\n");
                // Console.WriteLine("\nProduct Properties for vApp " + args[4] + "\n");
                // DeleteProductSection(vappRef, PRODUCT_SECTION_ID);

                // DisplayProductSections(vappRef);

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

        /// <summary>
        ///for testing purpose only, accept any certificate...
        /// </summary>
        private static bool ValidateServerCertificate(object sender, X509Certificate
        certificate, X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
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

