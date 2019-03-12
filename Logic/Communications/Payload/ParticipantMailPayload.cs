﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//using Swarmops.Basic.Types.Common;
using Swarmops.Logic.Security;
using Swarmops.Logic.Structure;
using Swarmops.Logic.Swarm;
using Swarmops.Logic.Support;

namespace Swarmops.Logic.Communications.Payload
{
    [Serializable]
    public class ParticipantMailPayload: PayloadBase<ParticipantMailPayload>, ICommsRenderer
    {
        [Obsolete("Do not call this constructor - doing so will generate this compile-time error. For serialization only.", true)]
        public ParticipantMailPayload()
        {
            // do not use - for serialization only
        }

        public SerializableDictionary<MailPayloadString, string> Strings{ get; set; }
        public string SubjectTemplate { get; set; }
        public string BodyTemplate { get; set; }

        public ParticipantMailPayload (ParticipantMailType mailType, Participation participation, Person actingPerson)
        {
            Strings = new SerializableDictionary<MailPayloadString, string>();

            switch (mailType)
            {
                case ParticipantMailType.ParticipantAddedWelcome:
                case ParticipantMailType.ParticipantAddedWelcome_NoExpiry:
                    Strings[MailPayloadString.ActingPerson] = actingPerson.Name;
                    Strings[MailPayloadString.MembershipExpiry] = participation.Expires.ToLongDateString();
                    Strings[MailPayloadString.OrganizationName] = participation.Organization.Name;
                    Strings[MailPayloadString.Regularship] =
                        Participant.Localized (participation.Organization.RegularLabel, TitleVariant.Ship);
                    Strings[MailPayloadString.ExternalUrl] = SystemSettings.ExternalUrl;

                    if (mailType == ParticipantMailType.ParticipantAddedWelcome)
                    {
                        BodyTemplate =
                            Resources.Logic_Communications_Transmission_DefaultCommTemplates
                                .ParticipantManualAddWelcome_Body;
                    }
                    else
                    {
                        BodyTemplate =
                            Resources.Logic_Communications_Transmission_DefaultCommTemplates
                                .ParticipantManualAddWelcome_NoExpiry_Body;
                    }

                    SubjectTemplate =
                        Resources.Logic_Communications_Transmission_DefaultCommTemplates
                            .ParticipantManualAddWelcome_Subject;

                    break;
                default:
                    throw new NotImplementedException("Unknown mailType: " + mailType.ToString());
            }
        }


        public ParticipantMailPayload (string customSubject, string customBody, Participation participation,
            Person actingPerson)
        {
            Strings = new SerializableDictionary<MailPayloadString, string>();

            SubjectTemplate = customSubject;
            BodyTemplate = customBody;
            Strings[MailPayloadString.ActingPerson] = actingPerson.Name;
            Strings[MailPayloadString.OrganizationName] = participation.Organization.Name;
        }


        public ParticipantMailPayload(string customSubject, string customBody, Organization organization,
            Person actingPerson)
        {
            Strings = new SerializableDictionary<MailPayloadString, string>();

            SubjectTemplate = customSubject;
            BodyTemplate = customBody;
            Strings[MailPayloadString.ActingPerson] = actingPerson.Name;
            Strings[MailPayloadString.OrganizationName] = organization.Name;
        }


        public RenderedComm RenderComm (Person person)
        {
            string body = BodyTemplate;
            string subject = SubjectTemplate;

            // If the template contains "[RandomPassword]", set the person's pwd to random and replace macro

            if (body.Contains ("[RandomPassword]"))
            {
                string randomPassword = Authentication.CreateWeakSecret (24);
                person.SetPassword (randomPassword);
                body = body.Replace ("[RandomPassword]", randomPassword);
            }

            // Replace each and every marker in template from lookup table

            foreach (
                MailPayloadString replacementString in (MailPayloadString[])Enum.GetValues(typeof(MailPayloadString))
                )
            {
                if (Strings.ContainsKey (replacementString))
                {
                    body = body.Replace ("[" + replacementString.ToString() + "]",
                        Strings[replacementString]);
                    subject = subject.Replace ("[" + replacementString.ToString() + "]",
                        Strings[replacementString]);
                }
            }

            if (!subject.StartsWith("[") && Strings.ContainsKey(MailPayloadString.OrganizationName))
            {
                // This is a little defensive programming as some subjects start with [Organization] and some don't - fix that here
                subject = "[" + Strings[MailPayloadString.OrganizationName] + "] " + subject;
            }

            RenderedComm result = new RenderedComm();
            result[CommRenderPart.BodyText] = body;
            result[CommRenderPart.Subject] = subject;
            result[CommRenderPart.SenderMail] = "admin@swarmops.com"; // TODO: FIX FIX FIX HACK
            result[CommRenderPart.SenderName] = "Swarmops Administrative";

            return result;
        }
    }


    public enum OrganizationMailSender
    {
        Unknown = 0,
        HumanResources,
        Accounting,
        OrganizationName,
        SystemNotifier,
        HighestExecutive
    }

    public enum ParticipantMailType
    {
        Unknown = 0,
        ParticipantAddedWelcome,
        ParticipantAddedWelcome_NoExpiry   
    }

    [Serializable]
    public enum MailPayloadString
    {
        // will likely need much more here

        Unknown = 0,
        MembershipExpiry,
        Regularship,
        ActingPerson,
        OrganizationName,
        ExternalUrl
    }

}
