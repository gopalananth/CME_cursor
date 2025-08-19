using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace Chef_Middle_East_Form.Models
{
    public class Form
    {
        /// <summary>
        /// Unique identifier for the lead record
        /// </summary>
        public string LeadId { get; set; }
        
        /// <summary>
        /// E-commerce related information
        /// Note: Keeping original property name for backward compatibility
        /// </summary>
        public string Ecomerce { get; set; }
        
        /// <summary>
        /// Properly spelled alias for Ecomerce property
        /// </summary>
        public string Ecommerce 
        { 
            get { return Ecomerce; } 
            set { Ecomerce = value; } 
        }
        
        /// <summary>
        /// Reason or context for the form submission
        /// </summary>
        public string Reason { get; set; }
        public string InventorySystem { get; set; }
        public DateTime? EmailSenton { get; set; }

        // Company Information Section
        /// <summary>
        /// Customer payment method selection - Required business field
        /// </summary>
        [Required(ErrorMessage = "Customer Payment Method is required")]
        [Display(Name = "Customer Payment Method")]
        public string CustomerPaymentMethod { get; set; } // Dropdown: Cash, Cheque, Credit

        /// <summary>
        /// Customer type classification - Optional business field
        /// </summary>
        [Display(Name = "Type")]
        public string Type { get; set; } // Dropdown: B2B (optional)

        /// <summary>
        /// Branch location - Mandatory business field as per requirements
        /// </summary>
        [Required(ErrorMessage = "Branch selection is required")]
        [Display(Name = "Branch")]
        public string Branch { get; set; } // Look Up: DXB (Mandatory)

        [Display(Name = "Company Name")]
        public string CompanyName { get; set; } // Optional

        [Display(Name = "Parent Account")]
        public string ParentAccount { get; set; } // Optional

        /// <summary>
        /// Trade name or outlet name as per trade license - Mandatory business field
        /// </summary>
        [Required(ErrorMessage = "Trade Name/Outlet Name is required as per trade license")]
        [StringLength(200, ErrorMessage = "Trade Name cannot exceed 200 characters")]
        [Display(Name = "Trade Name/Outlet Name( As per Trade license )")]
        public string TradeName { get; set; } // Mandatory

        [Display(Name = "Trade License Number")]
        public string TradeLicenseNumber { get; set; } // Optional

        [Display(Name = "License Expiry Date")]
        public DateTime? LicenseExpiryDate { get; set; } // Optional

        [Display(Name = "Mobile Phone")]
        [Phone]
        public string MainPhone { get; set; } // Optional

        // Additional property for compatibility with tests
        public string Phone 
        { 
            get { return MainPhone; } 
            set { MainPhone = value; } 
        }

        [Display(Name = "Email")]
        [EmailAddress]
        public string Email { get; set; } // Optional

        [Display(Name = "Price List")]
        public string PriceList { get; set; } // Optional Dropdown (Based on branch)

        [Display(Name = "Price Group")]
        public string PriceGroup { get; set; } // Optional Dropdown

        [Display(Name = "VAT Number")]
        public string VatNumber { get; set; } // Optional

        // Stakeholders
        [Display(Name = "Person in Charge First Name")]
        public string PersonInChargeFirstName { get; set; }

        [Display(Name = "Person in Charge Last Name")]
        public string PersonInChargeLastName { get; set; }

        [Display(Name = "Person in Charge Phone Number")]
        public string PersonInChargePhoneNumber { get; set; }

        [Display(Name = "Person in Charge EmailID")]
        public string PersonInChargeEmailID { get; set; }

        [Display(Name = "Person in Charge Role")]
        public string PersonInChargeRole { get; set; }


        [Display(Name = "Is Contact Person Same as Purchasing")]
        [Required(ErrorMessage = "This field is required.")]
        public string IsContactPersonSameAsPurchasing { get; set; } // Yes/No Choice

        [Display(Name = "Purchasing Person First Name")]
        public string PurchasingPersonFirstName { get; set; }

        [Display(Name = "Purchasing Person Last Name")]
        public string PurchasingPersonLastName { get; set; }

        [Display(Name = "Purchasing Person Phone Number")]
        public string PurchasingPersonPhoneNumber { get; set; }

        [Display(Name = "Purchasing Person EmailID")]
        public string PurchasingPersonEmailID { get; set; }

        [Display(Name = "Purchasing Person Role")]
        public string PurchasingPersonRole { get; set; }

        [Display(Name = "Is Contact Person Same as Company Owner")]
        [Required(ErrorMessage = "This field is required.")]
        public string IsContactPersonSameAsCompanyOwner { get; set; } // Yes/No Choice

        [Display(Name = "Company Owner First Name")]
        public string CompanyOwnerFirstName { get; set; }

        [Display(Name = "Company Owner Last Name")]
        public string CompanyOwnerLastName { get; set; }

        [Display(Name = "Company Owner Phone Number")]
        public string CompanyOwnerPhoneNumber { get; set; }

        [Display(Name = "Company Owner EmailID")]

        public string CompanyOwnerEmailID { get; set; }

        [Display(Name = "Company Owner Role")]

        public string CompanyOwnerRole { get; set; }

        // Corporate Address (Finance)
        [Display(Name = "Customer Name")]
        public string CorporateCustomerName { get; set; } // Optional

        [Display(Name = "Street")]
        public string CorporateStreet { get; set; } // Optional

        [Display(Name = "Country")]
        public string CorporateCountry { get; set; } // Optional Look Up

        [Display(Name = "City")]
        public string CorporateCity { get; set; } // Optional Look Up

        [Display(Name = "Shipping Method")]
        [Required(ErrorMessage = "This field is required.")]
        public string CorporateShippingMethod { get; set; } // Choice: Road, Air, Sea (Optional)

        // Delivery Address (Commercial)
        [Display(Name = "Customer Name")]
        public string DeliveryCustomerName { get; set; } // Optional

        [Display(Name = "Address 2: Street 1")]
        public string DeliveryStreet { get; set; } // Optional

        [Display(Name = "Delivery Country")]
        public string DeliveryCountry { get; set; } // Optional Look Up

        [Display(Name = "Delivery City")]
        public string DeliveryCity { get; set; } // Optional Look Up

        [Display(Name = "Address 2: Shipping Method")]
        [Required(ErrorMessage = "This field is required.")]
        public string DeliveryShippingMethod { get; set; } // Choice: Road, Air, Sea (Optional)

        // Registered Address
        [Display(Name = "Is Same as Corporate Address")]
        [Required(ErrorMessage = "This field is required.")]
        public string IsSameAsCorporateAddress { get; set; } // Choice: Yes/No (Optional)

        [Display(Name = "Customer Name")]
        public string RegisteredCustomerName { get; set; } // Optional

        [Display(Name = "Address3: Street")]
        public string RegisteredStreet { get; set; } // Optional

        [Display(Name = "Country (Registered Address)")]
        public string RegisteredCountry { get; set; } // Optional Look Up

        [Display(Name = "City (Registered Address)")]
        public string RegisteredCity { get; set; } // Optional Look Up

        // Primary Address
        // Additional Fields
        /// <summary>
        /// Statistical grouping for customer categorization - Mandatory business field
        /// </summary>
        [Required(ErrorMessage = "Statistic Group is required")]
        [Display(Name = "Statistic Group")]
        public string StatisticGroup { get; set; } // Mandatory Dropdown

        /// <summary>
        /// Chef customer segment classification - Mandatory business field
        /// </summary>
        [Required(ErrorMessage = "Chef Segment is required")]
        [Display(Name = "Chef Segment")]
        public string ChefSegment { get; set; } // Mandatory Dropdown

        /// <summary>
        /// Sub-segment classification within chef segment - Mandatory business field
        /// </summary>
        [Required(ErrorMessage = "Sub Segment is required")]
        [Display(Name = "Sub Segment")]
        public string SubSegment { get; set; } // Mandatory Dropdown

        [Display(Name = "Classification")]
        public string Classification { get; set; } // Optional Dropdown

        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; } // Choice: 21 Days EOM, Cash 7, IMMEDIATE, Net 45, 2% 10, Net 30, Net 30 (Optional)

        [Display(Name = "Proposed Payment Terms")]
        public string ProposedPaymentTerms { get; set; }

        [Display(Name = "Credit Limit")]
        public bool CreditLimit { get; set; }

        [Display(Name = "Requested Credit Limit")]
        public string RequestedCreditLimit { get; set; } // Optional


        [Display(Name = "Estimated Purchase Value")]
        public string EstimatedPurchaseValue { get; set; } // Optional

        [Display(Name = "Estimated Monthly Purchase")]
        public decimal? EstimatedMonthlyPurchase { get; set; } // Optional Currency

        [Display(Name = "Security Cheque Amount")]
        public decimal? SecurityChequeAmount { get; set; } // Optional Currency  changed from //Choice: Approved, Disapproved (Optional)

        [Display(Name = "Establishment Card Number")]
        public string EstablishmentCardNumber { get; set; }


        [Display(Name = "Cheque Copy")]
        public HttpPostedFileBase ChequeCopy { get; set; } // Mandatory if Approved

        [Display(Name = "Establishment Card Copy")]
        public HttpPostedFileBase EstablishmentCardCopy { get; set; }

        // Attachments
        [Display(Name = "VAT - TRN # (attach certificate)")]
        public HttpPostedFileBase VatCertificate { get; set; }

        [Display(Name = "Trade/Commercial License")]
        public HttpPostedFileBase TradeLicense { get; set; }

        [Display(Name = "Power of Attourney")]
        public HttpPostedFileBase POA { get; set; }

        [Display(Name = "Passport")]
        public HttpPostedFileBase Passport { get; set; }

        [Display(Name = "Visa")]
        public HttpPostedFileBase Visa { get; set; }

        [Display(Name = "National ID")]
        public HttpPostedFileBase EID { get; set; }

        [Display(Name = "Account Opening File")]
        public HttpPostedFileBase AccountOpeningFile { get; set; }

        public string ChequeCopyFileName { get; set; }
        public byte[] ChequeCopyFileData { get; set; }

        public string EstablishmentCardFileName { get; set; }
        public byte[] EstablishmentCardFileData { get; set; }

        public string VatCertificateFileName { get; set; }
        public byte[] VatCertificateFileData { get; set; }

        public string TradeLicenseFileName { get; set; }
        public byte[] TradeLicenseFileData { get; set; }

        public string POAFileName { get; set; }
        public byte[] POAFileData { get; set; }

        public string PassportFileName { get; set; }
        public byte[] PassportFileData { get; set; }

        public string VisaFileName { get; set; }
        public byte[] VisaFileData { get; set; }

        public string EIDFileName { get; set; }
        public byte[] EIDFileData { get; set; }

        public string AccountOpeningFileName { get; set; }
        public byte[] AccountOpeningFileData { get; set; }


        [Display(Name = "Last Visit")]
        public DateTime? LastVisit { get; set; }

        [Display(Name = "IBN/Account Number")]
        public string IBNAccountNumber { get; set; }

        [Display(Name = "Bank")]
        public string Bank { get; set; }

        [Display(Name = "Bank Name")]
        public string BankName { get; set; }

        [Display(Name = "Bank Address")]
        public string BankAddress { get; set; }

        [Display(Name = "Swift Code")]
        public string SwiftCode{ get; set; }

        [Display(Name = "Iban Number")]
        public string IbanNumber { get; set; }

        public string StatusCode { get; set; }

    }
}
