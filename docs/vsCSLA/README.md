# Neatoo vs CSLA: Concept Mapping & Skill Gap Analysis

This document uses Claude's extensive CSLA training knowledge as a catalog of features to identify Neatoo capabilities that exist but aren't documented in the Neatoo skill. When features are missing from the skill, Claude falls back to putting logic in the UI instead of using the framework.

**Key insight:** Neatoo + RemoteFactory = CSLA. The two skills together cover what CSLA does as one framework. Both should be assumed loaded together.

## How to Use This Document

- **Skill authors:** Use the gap analysis to prioritize skill improvements
- **Developers migrating from CSLA:** Use the concept mapping as a reference
- **AI assistants:** This document is NOT loaded as a skill — it's a design artifact

## Documents

| File | Purpose |
|------|---------|
| [csla-and-ddd.md](csla-and-ddd.md) | How CSLA relates to DDD, and where Neatoo bridges the gap |
| [concept-map.md](concept-map.md) | Side-by-side CSLA → Neatoo mapping for every major concept |
| [skill-gaps.md](skill-gaps.md) | Gaps found in the Neatoo skill, ranked by impact, with recommended fixes |
