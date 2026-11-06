# Scholaris — API Contract (v1)

Base URL: `http://localhost:8080` (docker) · Auth: `Authorization: Bearer <accessToken>` unless noted.
Errors: `{ "message": "..." }` with status 400/402/403/404/500. **402** payloads add
`courseId, price, discountPercent, finalPrice` → the UI routes the student to checkout.

## Auth
| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/auth/register` | `{fullName,email,password}` | `{accessToken,refreshToken,user{id,fullName,email,role}}` (role=Student) |
| POST | `/api/auth/login` | `{email,password}` | same |
| POST | `/api/auth/refresh` | `{refreshToken}` | same (rotation) |

## Catalog (public)
| Method | Path | Returns |
|---|---|---|
| GET | `/api/courses` | `[{id,slug,title,summary,subject,level,teacherName,price,discountPercent,finalPrice,coverHue,lessonCount,studentCount}]` |
| GET | `/api/courses/discounted` | same, only `discountPercent > 0` → powers the balloon announcement |
| GET | `/api/courses/{slug}` | detail + `modules[].topics[].lessons[]` (titles only) + `enrolled` |

## Payments (payer chooses the method)
| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/payments/methods` | — | `[{code,name,description,isMock}]` — mock: `telebirr_mock, cbebirr_mock, mpesa_mock, chapa_card_mock`; live: `chapa_live` |
| POST | `/api/payments/checkout` | `{courseId,methodCode}` | `{orderId,providerRef,instructions,amount,currency}` — order stores the **commission rate snapshot** |
| POST | `/api/payments/confirm` | `{orderId}` | `{orderId,status:"Paid",enrolled:true}` (mock completion; creates Enrollment) |
| POST | `/api/payments/webhook/{provider}` | provider payload | `200 {received:true}` — **real-provider integration point** (verify signature → verify with provider → mark Paid) |

## Enrollments & Learning (Student, paid access)
| Method | Path | Returns |
|---|---|---|
| GET | `/api/enrollments` | `[{courseId,slug,title,progressPercent,completedAt,certificateSerial}]` |
| GET | `/api/learn/{courseId}` | full course incl. lesson `content`/`videoYoutubeId` + `completedLessonIds` + `progressPercent` + `certificateSerial` (402 if not enrolled) |
| POST | `/api/learn/lessons/{lessonId}/complete` | `{progressPercent,courseCompleted,certificateSerial?}` — issues Certificate when the last lesson completes |

## AI exams — generated per student & per attempt, NEVER persisted
| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/ai-exam/generate` | `{topicId}` | `{examId,topicId,topicTitle,questions[{id,prompt,maxScore}]}` — held in memory 90 min only |
| POST | `/api/ai-exam/evaluate` | `{examId,answers[{questionId,text}]}` | `{scorePercent,overallFeedback,results[{questionId,verdict,score,maxScore,feedback}]}` — paper destroyed after grading |
| POST | `/api/ai/notes/summarize` | `{lessonId}` | `{summary,keyPoints[],elaborated}` — ephemeral |

## Q&A (Student ↔ Teacher)
| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/api/qa/threads` | `{courseId,lessonId?,question}` | thread |
| GET | `/api/qa/threads` | `?courseId=` optional | student: own threads · teacher(+courseId): course inbox |
| POST | `/api/qa/threads/{id}/answer` | `{answer}` | thread (teacher of the course only) |

## Certificates
| Method | Path | Returns |
|---|---|---|
| GET | `/api/certificates/mine` | student's certificates incl. template |
| GET | `/api/certificates/{serial}` | **public verification** `{serial,studentName,courseTitle,issuedAt,template}` |

## Admin back office (role=Admin)
| Method | Path | Body / Query | Returns |
|---|---|---|---|
| GET | `/api/admin/settings` | — | `{commissionRatePercent,siteName,updatedAt}` |
| PUT | `/api/admin/settings` | `{commissionRatePercent,siteName}` | updated settings |
| GET | `/api/admin/courses` | — | all courses incl. Draft/Archived |
| POST | `/api/admin/courses` | `{title,subject,level,summary,price,discountPercent,teacherId,coverHue,modules?}` — modules nested (`topics[] → lessons[]`) | created course |
| PUT | `/api/admin/courses/{id}` | same fields + `status` | updated course |
| DELETE | `/api/admin/courses/{id}` | — | 204 (refused if purchase history exists → archive instead) |
| GET | `/api/admin/teachers` | — | `[{id,fullName,email}]` |
| GET / PUT | `/api/admin/certificate-template` | template fields | `{title,subtitle,bodyTemplate,footerNote,signatoryName,signatoryTitle,accentHex}` |
| GET | `/api/admin/revenue` | `?year=` | `[{teacherName,month,gross,commission,net,orders}]` (month = `YYYY-MM`) |
| GET | `/api/admin/revenue/csv` | `?year=` | `text/csv` attachment |

## Teacher desk (role=Teacher)
| Method | Path | Returns |
|---|---|---|
| GET | `/api/teacher/courses` | own courses (for the Q&A inbox) |
| GET | `/api/teacher/revenue?year=` | own monthly rows |
| GET | `/api/teacher/revenue/csv?year=` | `text/csv` attachment |

## Money model (commission ledger)
```
finalPrice  = round(price × (1 − discountPercent/100))      ← discount set by Admin
commission  = round(finalPrice × commissionRateSnapshot/100) ← rate snapshot at payment
net         = finalPrice − commission                        ← teacher income
```
Revenue rows group **by teacher, by month** over paid orders; CSV mirrors the table.
