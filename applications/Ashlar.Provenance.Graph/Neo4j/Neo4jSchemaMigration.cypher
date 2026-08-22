// Idempotent schema migration for Ashlar provenance graph.
// Run once against a fresh or existing Neo4j database.

CREATE CONSTRAINT artifact_id IF NOT EXISTS
FOR (a:Artifact) REQUIRE a.id IS UNIQUE;

CREATE CONSTRAINT certificate_id IF NOT EXISTS
FOR (c:Certificate) REQUIRE c.id IS UNIQUE;

CREATE CONSTRAINT policy_version_id IF NOT EXISTS
FOR (p:PolicyVersion) REQUIRE p.id IS UNIQUE;

CREATE CONSTRAINT agent_id IF NOT EXISTS
FOR (g:Agent) REQUIRE g.id IS UNIQUE;

CREATE CONSTRAINT graph_metadata_id IF NOT EXISTS
FOR (m:GraphMetadata) REQUIRE m.id IS UNIQUE;
