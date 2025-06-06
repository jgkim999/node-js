package com.jgkim.movie.member;

import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

@Component
@RequiredArgsConstructor
public class MemberServiceImpl implements MemberService {
    private final MemberRepository memberRepository;

    /**
     * @param memberId 사용자 번호
     * @return Member
     */
    @Override
    public Member findMember(Long memberId) {
        return memberRepository.findById(memberId);
    }

    /**
     * @param member 사용자 번호
     */
    @Override
    public void registerMember(Member member) {
        memberRepository.save(member);
    }

    /**
     * @param memberId 사용자 번호
     */
    @Override
    public void removeMember(Long memberId) {
        memberRepository.delete(memberId);
    }

    /**
     * @param member 사용자
     */
    @Override
    public void modifyMember(Member member) {
        memberRepository.update(member);
    }
}
