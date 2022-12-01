// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.

namespace Visualizer.Shared.Models;

public record MentionRelationship(string FromUserId, string ToUserId, string TweetId, MentionRelationshipType RelationshipType);
