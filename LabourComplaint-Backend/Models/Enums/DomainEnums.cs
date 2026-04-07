// Models/Enums/DomainEnums.cs
namespace LabourComplaint_Backend.Models.Enums;

public enum UserRole { Citizen = 1, Inspector = 2, Admin = 3 }
public enum NotificationType { NewMessage = 1, ComplaintAssigned = 2, StatusUpdated = 3, DistrictBroadcast = 4, EscalationRequired = 5 }
public enum NotificationChannel { InApp = 1, Email = 2, Push = 3, Sms = 4 }
public enum NotificationStatus { Pending = 1, Sent = 2, Delivered = 3, Read = 4, Failed = 5 }
public enum PriorityLevel { Low = 1, Normal = 2, High = 3, Urgent = 4 }
public enum MessageDirection { CitizenToInspector = 1, InspectorToCitizen = 2, SystemToUser = 3 }
public enum ComplaintStatus { Draft = 1, Submitted = 2, Assigned = 3, UnderReview = 4, EvidenceRequested = 5, Resolved = 6, Closed = 7, Escalated = 8, Rejected = 9, Withdrawn = 10 }
public enum ChatRoomStatus { Open = 1, Closed = 2, Archived = 3 }
public enum EvidenceType { Photo = 1, Document = 2, Audio = 3, Video = 4, Link = 5 }
public enum EvidenceVisibility { PublicToCase = 1, InspectorOnly = 2, AdminOnly = 3 }