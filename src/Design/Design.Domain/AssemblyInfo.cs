// -----------------------------------------------------------------------------
// Design.Domain - Assembly-Level Attributes
// -----------------------------------------------------------------------------
// This file contains assembly-level attributes required for Neatoo source
// generation and other configuration.
// -----------------------------------------------------------------------------

using Neatoo.RemoteFactory;

// =============================================================================
// FactoryHintNameLength - Increase Generated File Name Limit
// =============================================================================
// RemoteFactory uses type fully qualified names for hint names when generating
// source files. The default limit is 50 characters. Longer namespaces/type
// names require increasing this limit to avoid truncation warnings (NF0104).
//
// The value should be at least as long as your longest FQN:
//   Design.Domain.FactoryOperations.CreateWithChildrenDemo = 64 characters
// =============================================================================

[assembly: FactoryHintNameLength(70)]
