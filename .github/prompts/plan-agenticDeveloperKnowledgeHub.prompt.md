## Plan: Agentic Developer Knowledge Hub

Upgrade the single-document console RAG app into a local-first agentic knowledge hub. A router agent will decide whether a software-development question should use attached documents, approved official documentation, or both. Specialist agents will research and analyze evidence, then a synthesizer will pass normalized evidence into the existing RAG path and return a cited, confidence-aware answer. Ollama and Qdrant remain the default runtime; provider interfaces keep Microsoft Foundry/Azure integration optional for a later deployment.

**Steps**

### Phase 1: Establish application boundaries
1. Move startup wiring out of the top-level `Program.cs` flow into explicit services and dependency-injection registration, while preserving the current CLI entry point and local Ollama/Qdrant configuration.
2. Introduce configuration options for Ollama, Qdrant, document paths, chunking, retrieval limits, and the official-domain allowlist; use environment variables or `appsettings.json` rather than source constants.
3. Split document ingestion from question answering. Support multiple PDF files, retain document identity, and make indexing idempotent or explicitly replaceable so stale vectors cannot contaminate answers.
4. Extend chunk metadata with source type, source URI/path, title, page number when available, retrieval timestamp, and a stable content/source identifier.

### Phase 2: Build the agent workflow
5. Define contracts and DTOs for `UserQuery`, `QueryRoute`, `EvidenceItem`, `ResourceCandidate`, `ResearchResult`, `AnalysisResult`, `AnswerDraft`, and `AgentExecutionTrace`.
6. Implement a router/planner agent that classifies intent and selects `AttachedDocuments`, `OfficialDocumentation`, or `Combined`; require software-development relevance and preserve the user’s original question.
7. Implement an official-resource researcher that uses curated Microsoft/.NET connectors and an allowlist, discovers candidate pages, validates URL/domain/type, fetches content, and records retrieval metadata. Keep external access behind an interface so a Foundry/web-search implementation can be added later.
8. Implement a resource-analysis agent that extracts claims, APIs, prerequisites, version/context constraints, and relevant evidence excerpts from each fetched resource. It must distinguish direct evidence from inference and reject unsupported claims.
9. Implement an agent coordinator that can run independent research/analysis branches concurrently, propagate cancellation and timeouts, capture failures per branch, and expose a trace explaining which agents and sources were used.
10. Normalize attached-document chunks and analyzed official documentation into one evidence model, then feed the normalized evidence into the RAG retrieval/synthesis pipeline. The final synthesizer must cite document names/page numbers or official URLs, state when evidence is insufficient or conflicting, and avoid presenting general model knowledge as verified fact.

### Phase 3: Make retrieval and answers production-shaped
11. Refactor `OllamaService` behind separate embedding and chat/generation abstractions so routing, specialist agents, and the final synthesizer can use structured prompts and JSON responses without coupling the workflow to Ollama’s HTTP DTOs.
12. Add source-aware Qdrant payloads and filtering. Index attached PDFs and accepted research resources with source identity/version metadata; avoid duplicate resource ingestion and prevent unapproved domains from entering the collection.
13. Add answer policy and citation formatting: source list, evidence snippets or references, confidence/grounding status, limitations, and a clear fallback when neither the attached material nor an approved source answers the question.
14. Add an optional provider adapter boundary for Microsoft Foundry/Azure models or agent-to-agent communication. Keep it disabled by default and document the required configuration; do not make Azure credentials a prerequisite for the local milestone.

### Phase 4: Verification and user workflow
15. Add a focused test project covering chunking/page metadata, route classification, allowlist and URL validation, evidence normalization, source deduplication, prompt/JSON parsing, insufficient-evidence behavior, and citation generation.
16. Add coordinator tests with fake agents/services to verify document-only, official-only, combined, failed-research, timeout, and conflicting-source paths without requiring Ollama, Qdrant, or network access.
17. Add a small evaluation fixture set of representative developer questions, expected route/source class, required citation behavior, and grounding constraints. Include a manual smoke-test script for local Ollama/Qdrant and one known official documentation question.
18. Update the README with architecture, agent responsibilities, trust boundaries, configuration, supported official domains, local setup, troubleshooting, and the optional Foundry integration path.

**Relevant files**
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/Program.cs` — reduce to CLI composition and command loop; call the application/coordinator service.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/OllamaService.cs` and `/Volumes/My Files/RAG-C#/RAGOnMyMac/IOllamaService.cs` — split embedding/generation responsibilities and add structured generation/provider contracts.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/QdrantVectorStore.cs` — add source metadata, filters, idempotent upsert/cleanup, and source-aware retrieval.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/Models/` — add query, route, evidence, source, answer, trace, and structured model-response types; extend `DocumentChunk` and `SearchResult` without losing existing fields.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/RAGOnMyMac.csproj` — add only the DI/configuration/testing or HTTP packages needed by the chosen implementation; keep local services external.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/Agents/` — router/planner, official researcher, resource analyzer, coordinator, and synthesizer implementations.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/Knowledge/` or `/Volumes/My Files/RAG-C#/RAGOnMyMac/Resources/` — PDF ingestion, official-source connectors, allowlist validation, fetching, normalization, and deduplication.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/Configuration/` — options and provider registration for local defaults plus optional Foundry configuration.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac.Tests/` — unit and coordinator tests with fakes; no network/service requirement for the default test run.
- `/Volumes/My Files/RAG-C#/RAGOnMyMac/README.md` — replace the single-PDF RAG description with the agentic architecture and operational instructions.

**Verification**
1. Run `dotnet build` after the structural refactor and again after the agent workflow is wired.
2. Run `dotnet test` with all tests using fakes; confirm no test requires Ollama, Qdrant, Azure credentials, or live internet.
3. Run the CLI with a document-only question and verify citations point to the attached PDF metadata.
4. Run an official-documentation question and verify only allowlisted Microsoft/.NET sources are accepted, with URL citations and retrieval timestamps.
5. Run a combined question and verify the coordinator merges both evidence types before final synthesis.
6. Exercise an unanswerable, conflicting-source, failed-fetch, and timeout case; verify the answer reports limitations rather than inventing a conclusion.
7. Perform a local smoke test with Ollama and Qdrant, then document the optional Foundry provider configuration separately.

**Decisions**
- First milestone is CLI-only; no web UI or HTTP API is included until the orchestration contract is stable.
- Local Ollama + Qdrant is the default runtime. Microsoft Foundry/Azure is an optional provider/deployment adapter, not a required dependency.
- Initial external research is limited to a configurable Microsoft/.NET official-domain allowlist. General open-web search is excluded from the first milestone.
- The system must prefer evidence and citations over broad model knowledge and must expose uncertainty, source conflicts, and missing evidence.
- Agent-to-agent communication is modeled as typed service contracts and coordinator messages in-process first; remote A2A transport is deferred to the optional Foundry phase.
- OCR, non-PDF formats, broad vendor coverage, autonomous write actions, and production deployment are out of scope for this milestone.

**Further Considerations**
1. The next implementation pass should choose whether official-page discovery is a static sitemap/API connector or a user-provided URL plus curated index. Recommendation: start with a small configurable catalog of Microsoft Learn/.NET URL patterns and add broader discovery only after citation and grounding tests pass.
2. Structured local-model output should use strict JSON schemas with repair/failure handling; prompts alone are insufficient as a trust boundary.
3. Before enabling an Azure/Foundry adapter, add provider contract tests so local and hosted agents produce the same route/evidence/answer shapes.
