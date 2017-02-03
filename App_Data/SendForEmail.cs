/// <summary>
		/// Initiates the signing process for the provided letter with the provided Signer information.
		/// </summary>
		/// <param name="letter">The letter for which to initiate signing.</param>
		/// <param name="signers">The collection of Signers for the provided letter.</param>
		/// <returns>Returns the eSignature tracking number associated with the document.</returns>
		public string SendForSignature(ILetter letter, IEnumerable<ISigner> signers)
		{
            EnvelopeDefinition envDef = new EnvelopeDefinition();
            envDef.CompositeTemplates = new List<CompositeTemplate>();
            CompositeTemplate compTemp = new CompositeTemplate();
            compTemp.InlineTemplates = new List<InlineTemplate>();
            InlineTemplate inlineTemp = new InlineTemplate();
            Random docId = new Random();
            int recipientId = 0;
            int sequence = 0;

            string accountId = loginApi(_ServiceAccountUsername, _ServiceAccountPassword, _SinatureKey);

            //Define variables used to structure data and submit request
            //Define Envelope Data
            envDef.EmailSubject = "Please Sign this document";
            envDef.EmailBlurb = "Please Sign this document";
            envDef.Status = "sent";

            //Get Document Data
            byte[] fileBytes = letter.Content;

            //Define Document Data
            compTemp.Document = new Document();
            compTemp.Document.DocumentId = docId.Next(1, 99999).ToString();
            compTemp.Document.Name = letter.LetterIdentifier;
            compTemp.Document.DocumentBase64 = Convert.ToBase64String(fileBytes);
            compTemp.Document.TransformPdfFields = "true";

            //Define Signer Data
            foreach (ISigner isigner in signers)
            {
                recipientId++;
                sequence++;
                Signer signer = new Signer();
                SignHere signHere = new SignHere();
                inlineTemp.Recipients = new Recipients();

                signer.Email = isigner.EmailAddress;
                signer.Name = string.Format("{0}", isigner.FullName);
                signer.RecipientId = recipientId.ToString();
                signer.Tabs = new Tabs();
                signer.Tabs.SignHereTabs = new List<SignHere>();

                //Define Signature Tab Area
                signHere.DocumentId = compTemp.Document.DocumentId;
                signHere.RecipientId = recipientId.ToString();
                if (recipientId == 1)
                {
                    signHere.AnchorString = "<<PwCSigner>>";
                }
                else if (recipientId == 2)
                {                    
                    signHere.AnchorString = "<<ClientSigner>>";
                }
                signHere.AnchorIgnoreIfNotPresent = "True";
                signer.Tabs.SignHereTabs.Add(signHere);

                //Define Recipients
                inlineTemp.Recipients.Signers = new List<Signer>();
                inlineTemp.Recipients.Signers.Add(signer);
                inlineTemp.Sequence = sequence.ToString();
            }

            //"Glue" final data structure request together
            compTemp.InlineTemplates.Add(inlineTemp);
            envDef.CompositeTemplates.Add(compTemp);

            //Define header credentials
            string authString = "{\"Username\":\"" + _ServiceAccountUsername + "\", \"Password\":\"" + _ServiceAccountPassword + "\", \"IntegratorKey\":\"" + _SinatureKey + "\"}";
            ApiClient apiClient = new ApiClient(basePath: _ServiceUrl);
            Configuration cfg = new Configuration(apiClient);
            cfg.AddDefaultHeader("X-DocuSign-Authentication", authString);

            //Execute Signature Request Call
            EnvelopesApi envelopesApi = new EnvelopesApi(cfg);
            EnvelopeSummary envelopeSummary = envelopesApi.CreateEnvelope(accountId, envDef);
            return envelopeSummary.EnvelopeId;
		}