﻿// ******************************************************************************
//  © 2019 Sebastiaan Dammann | damsteen.nl
// 
//  File:           : GetRetrospectiveLaneContentQueryHandler.cs
//  Project         : Return.Application
// ******************************************************************************

namespace Return.Application.RetrospectiveLanes.Queries {
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using Common.Abstractions;
    using Common.Models;
    using Domain.Entities;
    using Domain.Services;
    using MediatR;
    using Microsoft.EntityFrameworkCore;
    using Services;

    public sealed class GetRetrospectiveLaneContentQueryHandler : IRequestHandler<GetRetrospectiveLaneContentQuery, RetrospectiveLaneContent> {
        private readonly IReturnDbContext _returnDbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentParticipantService _currentParticipantService;
        private readonly ITextAnonymizingService _textAnonymizingService;

        public GetRetrospectiveLaneContentQueryHandler(IReturnDbContext returnDbContext, IMapper mapper, ICurrentParticipantService currentParticipantService, ITextAnonymizingService textAnonymizingService) {
            this._returnDbContext = returnDbContext;
            this._mapper = mapper;
            this._currentParticipantService = currentParticipantService;
            this._textAnonymizingService = textAnonymizingService;
        }

        public async Task<RetrospectiveLaneContent> Handle(GetRetrospectiveLaneContentQuery request, CancellationToken cancellationToken) {
            if (request == null) throw new ArgumentNullException(nameof(request));

            Retrospective retrospective = await this._returnDbContext.Retrospectives.AsNoTracking().FindByRetroId(request.RetroId, cancellationToken).ConfigureAwait(false);

            var laneId = (KnownNoteLane)request.LaneId;
            var query =
                from note in this._returnDbContext.Notes
                where note.Retrospective.UrlId.StringId == request.RetroId
                where note.Lane.Id == laneId
                orderby note.CreationTimestamp
                select note;

            var lane = new RetrospectiveLaneContent();
            lane.Notes.AddRange(
                await query.ProjectTo<RetrospectiveNote>(this._mapper.ConfigurationProvider).ToListAsync(cancellationToken).ConfigureAwait(false)
            );

            int currentUserId = await this._currentParticipantService.GetParticipantId().ConfigureAwait(false);
            foreach (RetrospectiveNote note in lane.Notes) {
                note.IsOwnedByCurrentUser = currentUserId == note.ParticipantId;

                if (retrospective.CurrentStage < RetrospectiveStage.Discuss && !note.IsOwnedByCurrentUser) {
                    if (note.Text != null) {
                        note.Text = this._textAnonymizingService.AnonymizeText(note.Text);
                    }
                }
            }

            return lane;
        }
    }
}
