
using Shared.Models;
using ChatService.Repositories;
using DbChatMember = ChatService.Models.ChatMember;

namespace ChatService.Services;

public class ChatMemberService : IChatMemberService
{
    private readonly IChatMemberRepository _memberRepository;
    private readonly IChatRepository _chatRepository;

    public ChatMemberService(IChatMemberRepository memberRepository, IChatRepository chatRepository)
    {
        _memberRepository = memberRepository;
        _chatRepository = chatRepository;
    }

    private ChatMember MapToShared(DbChatMember dbMember)
    {
        return new ChatMember
        {
            Id = dbMember.Id,
            CompanyId = dbMember.CompanyId,
            ChatId = dbMember.ChatId,
            UserId = dbMember.UserId,
            JoinedAt = dbMember.JoinedAt
        };
    }

    private DbChatMember MapToDb(ChatMember member)
    {
        return new DbChatMember
        {
            Id = member.Id,
            CompanyId = member.CompanyId,
            ChatId = member.ChatId,
            UserId = member.UserId,
            JoinedAt = member.JoinedAt
        };
    }

    public async Task<ChatMember> AddMemberAsync(string chatId, int userId, int companyId)
    {
        var member = new ChatMember
        {
            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            ChatId = chatId,
            UserId = userId,
            CompanyId = companyId,
            JoinedAt = DateTime.UtcNow
        };

        var dbMember = MapToDb(member);
        await _memberRepository.AddMemberAsync(dbMember);
        await _chatRepository.AddMemberIdAsync(chatId, userId);
        
        return member;
    }

    public async Task<bool> RemoveMemberAsync(string chatId, int userId, int companyId)
    {
        if (!await IsMemberAsync(chatId, userId, companyId))
            return false;

        await _memberRepository.RemoveMemberAsync(chatId, userId);
        await _chatRepository.RemoveMemberIdAsync(chatId, userId);
        
        return true;
    }

    public async Task<List<ChatMember>> GetByChatIdAsync(string chatId, int companyId)
    {
        var members = await _memberRepository.GetByChatIdAsync(chatId);
        return members.Where(m => m.CompanyId == companyId).Select(MapToShared).ToList();
    }

    public async Task<bool> IsMemberAsync(string chatId, int userId, int companyId)
    {
        var member = await _memberRepository.GetMemberAsync(chatId, userId);
        return member != null && member.CompanyId == companyId;
    }
}






