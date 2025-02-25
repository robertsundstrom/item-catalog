﻿
using Catalog.Application.Common.Interfaces;
using Catalog.Domain.Entities;

using MediatR;

namespace Catalog.Application.Users.Commands;

public class CreateUserCommand : IRequest<UserDto>
{
    public CreateUserCommand(string? id, string firstName, string lastName, string? displayName, string email)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        DisplayName = displayName;
        Email = email;
    }

    public string? Id { get; }

    public string FirstName { get; }

    public string LastName { get; }

    public string? DisplayName { get; }

    public string Email { get; }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
    { 
        readonly ICatalogContext _context;

        public CreateUserCommandHandler(ICatalogContext context)
        {
            _context = context;
        }

        public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var user = new User
            {
                Id = request.Id ?? Guid.NewGuid().ToString(),
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = request.DisplayName,
                Email = request.Email
            };

            _context.Users.Add(user);

            await _context.SaveChangesAsync(cancellationToken);

            return new UserDto(user.Id, user.FirstName, user.LastName, user.DisplayName, user.Email, user.Created, user.LastModified);
        }
    }
}