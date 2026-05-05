# Business Requirements Document (BRD)

## RFx Sourcing Event Management

---

### Document Control

| Field            | Value                                                                 |
| ---------------- | --------------------------------------------------------------------- |
| Document Title   | BRD — RFx Sourcing Event Management                                   |
| Version          | 0.1 (Draft)                                                           |
| Status           | For Review                                                            |
| Author           | Ramkumar (Facilitator)                                                |
| Date             | 04 May 2026                                                           |
| Intended Audience | Business Stakeholders, Development Team, Solution Architects         |
| Related Programme | Professional AI-Assisted Development — Training Delivery             |

---

### 1. Document Purpose

This document captures the **business intent, scope, and functional expectations** of an RFx (Request-for-X) Sourcing Event Management capability. It is intended to serve as the shared reference for business stakeholders and the development team during scoping, design, and validation.

Technical design, architecture, data model, API contracts, and non-functional sizing details are intentionally deferred to subsequent artefacts (HLD, LLD, API Specification).

---

### 2. Executive Summary

Procurement organisations routinely run **sourcing events** to solicit, evaluate, and select supplier offers for goods and services. Today, a meaningful portion of this activity is still managed through spreadsheets, email threads, and disconnected tools — making the process slow, error-prone, and difficult to audit.

This initiative introduces a **digital RFx Sourcing Event capability** that allows a buyer to create a sourcing event, invite participating suppliers, capture supplier bids, evaluate them against weighted criteria, and award the event to the chosen supplier — all in a single, auditable workflow.

The capability is the foundation on which more advanced sourcing features (multi-round negotiation, AI-assisted scoring, contract initiation) will be layered in future releases.

---

### 3. Business Context & Background

Sourcing is one of the most critical activities within the procurement function. A well-run sourcing event directly influences cost savings, supplier quality, delivery performance, and risk exposure.

In a typical enterprise:

- A **buyer** (Category Manager) identifies a need to procure a category of goods or services.
- The buyer issues an RFx — a structured request inviting suppliers to respond with their commercial and technical offers.
- Suppliers respond with bids covering price, lead time, terms, and other criteria.
- The buyer evaluates bids against pre-defined criteria and **awards** the business to the most suitable supplier.

The pain points addressed by this capability:

- Lack of a single source of truth for sourcing events and bids.
- Manual, inconsistent scoring leading to disputed award decisions.
- Limited audit trail of who did what, when.
- Delays caused by email-based supplier coordination.

---

### 4. Business Objectives

The capability is expected to deliver the following business outcomes:

- **B-OBJ-01** — Provide buyers with a single, structured workflow to run a sourcing event end-to-end.
- **B-OBJ-02** — Reduce the manual effort required to collect, organise, and compare supplier bids.
- **B-OBJ-03** — Improve transparency and auditability of supplier evaluation and award decisions.
- **B-OBJ-04** — Establish a clean foundation for future enhancements such as AI-assisted scoring and multi-round negotiation.
- **B-OBJ-05** — Demonstrate, within a training context, how a focused procurement feature can be delivered using modern AI-assisted development practices.

---

### 5. Scope

#### 5.1 In Scope

The following capabilities are **in scope** for this release:

- Creation of a new RFx sourcing event with header information (title, category, currency, response deadline).
- Addition of one or more **line items** to the event (item description, quantity, unit of measure).
- Definition of **scoring criteria** with weights (e.g., price, lead time, quality).
- Invitation of one or more suppliers to participate in the event.
- **Publishing** of the event, after which bids may be captured.
- **Capturing** supplier bids against the published event.
- **Scoring** of submitted bids against the defined criteria, producing a ranked list.
- **Awarding** the event to the selected supplier, with the award decision recorded.
- Viewing the **status** of an event at any point in its lifecycle.
- **Authenticated access** to ensure only authorised buyers can act on events.

#### 5.2 Out of Scope

The following are **explicitly excluded** from this release and may be considered for future iterations:

- Multi-round negotiation and reverse auctions.
- Automated supplier notifications via email or other channels.
- A self-service supplier portal for bid submission.
- Contract generation or e-signature workflows post-award.
- Spend analytics, savings tracking, and reporting dashboards.
- Mobile-specific user experience.
- Multi-tenant or multi-organisation support.
- Supplier onboarding and risk scoring (separate capability).
- Integration with downstream ERP, P2P, or financial systems.
- AI-assisted recommendations within the scoring engine.

---

### 6. Stakeholders

| Stakeholder              | Role in the Process                                                  |
| ------------------------ | -------------------------------------------------------------------- |
| Category Manager / Buyer | Primary user — creates events, invites suppliers, awards business.   |
| Sourcing Lead            | Reviews and oversees award decisions.                                |
| Supplier                 | Submits bids in response to invitations (no portal in this release). |
| Procurement Director     | Provides governance and oversight of sourcing outcomes.              |
| Development Team         | Delivers and supports the capability.                                |
| Training Participants    | Observe the demonstration and apply the practices to a fresh case.   |

---

### 7. Business Process Flow

The end-to-end business process supported by this capability:

1. **Identify Need** — A buyer identifies a category of goods/services to be sourced.
2. **Create Event** — The buyer creates a new RFx event, captures header information and line items.
3. **Define Criteria** — The buyer defines the scoring criteria and weights for evaluation.
4. **Invite Suppliers** — The buyer adds the list of participating suppliers to the event.
5. **Publish Event** — The buyer publishes the event, locking the structure and opening it for bids.
6. **Capture Bids** — Bids are recorded against the event for each invited supplier.
7. **Score Bids** — The system computes a weighted score for each bid against the defined criteria.
8. **Rank & Review** — The buyer reviews the ranked list of bids.
9. **Award Event** — The buyer awards the event to the chosen supplier; the decision is recorded.
10. **Close Event** — The event moves to a closed/awarded state and becomes part of the audit record.

---

### 8. Functional Requirements (Business-Level)

| Ref     | Requirement                                                                                       |
| ------- | ------------------------------------------------------------------------------------------------- |
| FR-01   | A buyer shall be able to create a new RFx event with title, category, currency, and deadline.    |
| FR-02   | A buyer shall be able to add one or more line items to an event.                                  |
| FR-03   | A buyer shall be able to define scoring criteria with associated weights for an event.            |
| FR-04   | A buyer shall be able to add one or more suppliers to the event invitation list.                  |
| FR-05   | A buyer shall be able to publish an event, transitioning it from Draft to Published.              |
| FR-06   | The system shall accept bids for a published event from invited suppliers.                        |
| FR-07   | The system shall score submitted bids against the defined criteria and weights.                   |
| FR-08   | The system shall produce a ranked list of bids for a given event.                                 |
| FR-09   | A buyer shall be able to award an event to a chosen supplier, with the rationale recorded.        |
| FR-10   | A buyer shall be able to view the current status and details of any event they own.               |
| FR-11   | The system shall require authenticated access for all buyer actions.                              |
| FR-12   | The system shall maintain a basic audit trail of key actions (create, publish, bid, score, award).|

---

### 9. Non-Functional Expectations (Indicative)

These are indicative business expectations only. Detailed non-functional requirements will be defined in subsequent design artefacts.

- **Security** — Buyer actions must be authenticated; unauthenticated requests must be rejected.
- **Auditability** — Key lifecycle actions on an event must be traceable.
- **Reliability** — The capability should behave predictably for the demonstration scope; resilience and high-availability are not in scope for this release.
- **Usability** — The interfaces should be straightforward enough for a buyer to follow the lifecycle without extensive training.
- **Maintainability** — The solution should be structured to allow future enhancements (e.g., AI scoring, supplier portal) without rework.

---

### 10. Assumptions

- A single buyer organisation is assumed; multi-tenancy is not required.
- A small, pre-defined list of suppliers is available; supplier self-registration is not required.
- Bids are submitted on behalf of suppliers via a controlled channel; a supplier-facing UI is not part of this release.
- Award decisions made within the system are non-binding and serve a demonstration purpose.
- Scoring criteria are simple weighted attributes; advanced multi-attribute decision techniques are not required.
- All financial values are expressed in a single currency (INR) for the demonstration.
- Email or other external notifications are managed outside the system.

---

### 11. Constraints

- The capability is intended to run in a **local environment** for demonstration purposes — no cloud deployment in this release.
- The build window is limited to **12 hours of demonstration time**, requiring tight scope discipline.
- The user interface for this release is limited to API-level interaction (e.g., via Postman); no rich front-end is included.
- Only the English language is supported.

---

### 12. Success Criteria

The release will be considered successful if:

- All in-scope functional requirements (FR-01 to FR-12) are demonstrable end-to-end.
- A buyer can move an event through the full lifecycle — Create → Invite → Publish → Bid → Score → Award — without manual intervention or workarounds.
- The award decision is recorded and traceable.
- The demonstration runs cleanly in front of a live audience without unresolved errors.
- Training participants can articulate the RFx process and apply the same approach to a different use case provided to them post-demonstration.

---

### 13. Risks & Mitigations

| Risk                                                                  | Likelihood | Impact | Mitigation                                                                  |
| --------------------------------------------------------------------- | ---------- | ------ | --------------------------------------------------------------------------- |
| Scope creep into multi-round negotiation or contract generation.      | Medium     | High   | Strict adherence to the in-scope/out-of-scope definitions in this BRD.      |
| Time pressure during the live demonstration causes incomplete flow.   | Medium     | High   | Pre-prepared scaffolding and rehearsal of the demonstration.                |
| Sample supplier or bid data is unrealistic for stakeholders.          | Low        | Medium | Use representative, plausible sample data aligned to a familiar category.   |
| Stakeholders expect a polished UI experience.                         | Medium     | Medium | Communicate up-front that this release is API-driven and UI is out of scope.|

---

### 14. Glossary

| Term              | Definition                                                                                  |
| ----------------- | ------------------------------------------------------------------------------------------- |
| RFx               | Umbrella term for Request-for-X, where X is Information (RFI), Proposal (RFP), or Quotation (RFQ). |
| Sourcing Event    | A structured exercise to solicit and evaluate supplier offers for a defined need.            |
| Buyer             | The procurement professional running the sourcing event.                                    |
| Supplier          | An external organisation invited to respond with a bid.                                     |
| Bid               | A supplier's response to an event, containing commercial and technical details.             |
| Line Item         | A single good or service being sourced as part of an event.                                 |
| Scoring Criteria  | The attributes (e.g., price, lead time, quality) used to evaluate bids, each with a weight. |
| Award             | The final decision selecting a supplier as the winner of the event.                         |

---

### 15. Document History

| Version | Date        | Author    | Notes                |
| ------- | ----------- | --------- | -------------------- |
| 0.1     | 04 May 2026 | Ramkumar  | Initial draft for review. |

---

*End of Document*
