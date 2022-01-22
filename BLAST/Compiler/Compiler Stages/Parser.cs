using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.Mathematics;

namespace NSS.Blast.Compiler.Stage
{

    /// <summary>
    /// The Parser: 
    /// 
    /// - Parses list of tokens into a tree of nodes representing the flow of operations  
    /// - Identifies unique parameters 
    /// - Spaghetti warning - handcrafted parser ahead..
    /// 
    /// </summary>
    public class BlastParser : IBlastCompilerStage
    {
        public System.Version Version => new System.Version(0, 2, 1);
        public BlastCompilerStageType StageType => BlastCompilerStageType.Parser;


        /// <summary>
        /// scan for the next token of type 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token">token to look for</param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max idx to look into</param>
        /// <param name="i1">idx of token</param>
        /// <returns>true if found</returns>
        bool find_next(IBlastCompilationData data, BlastScriptToken token, int idx, in int max, out int i1)
        {
            while (idx <= max)
            {
                if (data.Tokens[idx].Item1 != token)
                {
                    // as its more likely to not match to token take this branch first 
                    idx++;
                }
                else
                {
                    i1 = idx;
                    return true;
                }
            }
            i1 = -1;
            return false;
        }

        /// <summary>
        /// scan for the next token of type 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token">token to look for</param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max idx to look into</param>
        /// <param name="i1">idx of token</param>
        /// <returns>true if found</returns>
        bool find_next(IBlastCompilationData data, BlastScriptToken[] tokens, int idx, in int max, out int i1)
        {
            while (idx <= max)
            {
                if (!tokens.Contains(data.Tokens[idx].Item1))
                {
                    // as its more likely to match to token take this branch first 
                    idx++;
                }
                else
                {
                    i1 = idx;
                    return true;
                }
            }
            i1 = -1;
            return false;
        }

        /// <summary>
        /// search for the next token skipping over compounds 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token">token to look for</param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max idx to look into</param>
        /// <param name="i1">idx of token</param>
        /// <returns>true if found</returns>
        bool find_next_skip_compound(IBlastCompilationData data, BlastScriptToken token, int idx, in int max, out int i1, bool accept_eof = false)
        {
            int size = max - idx;
            int open = 0;

            while (idx <= max)
            {
                BlastScriptToken t = data.Tokens[idx].Item1;

                if (t == BlastScriptToken.OpenParenthesis)
                {
                    open++;
                    idx++;
                    continue;
                }

                if (t == BlastScriptToken.CloseParenthesis)
                {
                    if (open > 0)
                    {
                        open--;
                        idx++;
                        continue;
                    }
                }

                if (t == token)
                {
                    if (open != 0)
                    {
                        data.LogError($"parser.find_next_skip_compound: malformed parenthesis between idx {idx} and {max}");
                        i1 = -1;
                        return false;
                    }

                    i1 = idx;
                    return true;
                }
                else
                {
                    idx++;
                }
            }
            if (accept_eof)
            {
                if (size > 1)
                {
                    i1 = max;
                    return true;
                }
            }
            i1 = -1;
            return false;
        }

        /// <summary>
        /// search for the next token skipping over compounds 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token">token to look for</param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max idx to look into</param>
        /// <param name="i1">idx of token</param>
        /// <returns>true if found</returns>
        bool find_next_skip_compound(IBlastCompilationData data, BlastScriptToken[] tokens, int idx, in int max, out int i1, bool accept_eof = false)
        {
            int size = max - idx;
            int open = 0;

            while (idx <= max)
            {
                BlastScriptToken t = data.Tokens[idx].Item1;

                if (t == BlastScriptToken.OpenParenthesis)
                {
                    open++;
                    idx++;
                    continue;
                }

                if (t == BlastScriptToken.CloseParenthesis)
                {
                    if (open > 0)
                    {
                        open--;
                        idx++;
                        continue;
                    }
                }

                if (tokens.Contains(t))
                {
                    if (open != 0)
                    {
                        data.LogError($"parser.find_next_skip_compound: malformed parenthesis between idx {idx} and {max}");
                        i1 = -1;
                        return false;
                    }

                    i1 = idx;
                    return true;
                }
                else
                {
                    idx++;
                }
            }
            if(accept_eof)
            {
                if(size > 1)
                {
                    i1 = max; 
                    return true;
                }
            }
            i1 = -1;
            return false;
        }

        /// <summary>
        /// find next token from idx 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="token"></param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max index to check into</param>
        /// <param name="i1">token location or -1 if not found</param>
        /// <param name="skip_over_compounds">skip over ( ) not counting any token inside the (compound)</param>
        /// <param name="accept_eof">accept eof as succesfull end of search</param>
        /// <returns></returns>
        bool find_next(IBlastCompilationData data, BlastScriptToken token, int idx, in int max, out int i1, bool skip_over_compounds = true, bool accept_eof = true)
        {
            bool found;

            if (skip_over_compounds)
            {
                found = find_next_skip_compound(data, token, idx, max, out i1, accept_eof);
            }
            else
            {
                found = find_next(data, token, idx, max, out i1, false, accept_eof);
            }

            // accepting eof?? 
            if (!found && idx >= max && accept_eof)
            {
                i1 = max;
                found = true;
            }

            return found;
        }

        /// <summary>
        /// find next match in token array 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="tokens"></param>
        /// <param name="idx">idx to start looking from</param>
        /// <param name="max">max index to check into</param>
        /// <param name="i1">token location or -1 if not found</param>
        /// <param name="skip_over_compounds">skip over ( ) not counting any token inside the (compound)</param>
        /// <param name="accept_eof">accept eof as succesfull end of search</param>        /// <returns></returns>
        bool find_next(IBlastCompilationData data, BlastScriptToken[] tokens, int idx, in int max, out int i1, bool skip_over_compounds = true, bool accept_eof = true)
        {
            bool found;

            if (skip_over_compounds)
            {
                found = find_next_skip_compound(data, tokens, idx, max, out i1, accept_eof);
            }
            else
            {
                found = find_next(data, tokens, idx, max, out i1, false, accept_eof);
            }

            // accepting eof?? 
            if (!found && idx >= max && accept_eof)
            {
                i1 = max;
                found = true;
            }

            return found;
        }


        /// <summary>
        /// skip the closure () starting with idx at the (, if true ends with idx at position after ) 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        bool skip_closure(IBlastCompilationData data, ref int idx, int idx_max)
        {
            if (idx > idx_max || data.Tokens[idx].Item1 != BlastScriptToken.OpenParenthesis)
            {
                data.LogError($"parser.skip_closure: expecting parenthesis () but found {data.Tokens[idx].Item1}");
                return false;
            }

            // find IF closure )
            if (!find_next(data, BlastScriptToken.CloseParenthesis, idx + 1, idx_max, out idx, true, false))
            {
                data.LogError($"parser.skip_closure: malformed parenthesis");
                return false;
            }

            // read 1 past if possible
            if (idx <= idx_max)
            {
                if (idx < idx_max) idx++;
                return true;
            }
            else
            {
                idx++;
                return false;
            }
        }

        /// <summary>
        /// scan token tree and find start and end index of next statement in token list
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx">current index into token list</param>
        /// <param name="i1">start index of next statement</param>
        /// <param name="i2">end index of next statement</param>
        bool find_next_statement(IBlastCompilationData data, ref int idx, int idx_max, out int i1, out int i2)
        {
            i1 = i2 = -1;
            if (!(idx <= idx_max))
            {
                return false;
            }

            if (idx == idx_max)
            {
                // either single token or could not go past 

            }

            // find start of statement 
            //bool end_on_id = false;
            while (idx <= idx_max && i1 < 0)
            {
                switch (data.Tokens[idx].Item1)
                {
                    case BlastScriptToken.DotComma:
                        data.LogWarning($"parser.FindNextStatement: skipping ';'");
                        idx++;
                        continue;

                    case BlastScriptToken.Nop:
                        idx++;
                        continue;

                    case BlastScriptToken.CloseParenthesis:
                         idx++;
                         data.LogWarning($"TODO REMOVE parser.findnextstatement debug close )");
                        return false;

                    case BlastScriptToken.Switch:
                    case BlastScriptToken.While:
                    case BlastScriptToken.For:
                    case BlastScriptToken.If:
                    case BlastScriptToken.Identifier:
                        i1 = idx;
                        //if(idx == idx_max)
                       // {
                       //     // allow to end on a identifier 
                        //    end_on_id = true; 
                        //}
                        break;

                    default:
                        // error: token not valid in currenct context
                        data.LogError($"parser.FindNextStatement: found invalid token '{data.Tokens[idx].Item1}' while looking for statement start.");
                        i1 = i2 = -1;
                        idx = idx_max + 1;
                        return false;
                }
            }

            // if we reached the end nothing usefull was found
            if (idx >= idx_max)
            {
                i1 = i2 = -1;
                return false;
            }

            // depending on the starting token of the statement we can end up with different flows 
            switch (data.Tokens[i1].Item1)
            {

                case BlastScriptToken.Identifier:
                    // any statement starting with an identifier can be 2 things:
                    // - function called from root returning nothing
                    // - assignment of identifier
                    // - flow is terminated with a ';' or eof
                    // - a statement wont ever start with a -
                    // - a statement can contain (compounds)

                    if (find_next(data, new BlastScriptToken[] { BlastScriptToken.DotComma, BlastScriptToken.CloseParenthesis }, i1 + 1, idx_max, out i2, true, true))
                    {
                        // read the currentindex past the last part of the statement which is now
                        // contained in tokens[i1-i2]  
                        idx = i2 + 1;

                        // allow ;) read past the parenthesiss
                        if (idx > 0
                            &&
                            idx < data.Tokens.Count - 1
                            &&
                            data.Tokens[idx - 1].Item1 == BlastScriptToken.DotComma
                            &&
                            data.Tokens[idx].Item1 == BlastScriptToken.CloseParenthesis)
                        {
                            idx++;
                        }
                        return true;
                    }
                    break;


                case BlastScriptToken.If:
                    // if then else statements: 
                    // - they always start the statement
                    // - then and else optional but need at least 1  

                    // next token should open IF closure with (
                    idx++;
                    if (idx > idx_max || data.Tokens[idx].Item1 != BlastScriptToken.OpenParenthesis)
                    {
                        data.LogError($"parser.FindNextStatement: expecting parenthesis () after IF but found {data.Tokens[idx].Item1}");
                        return false;
                    }

                    // find IF closure )
                    if (!find_next(data, BlastScriptToken.CloseParenthesis, idx + 1, data.Tokens.Count - 1, out idx, true, false))
                    {
                        data.LogError($"parser.FindNextStatement: malformed parenthesis in IF condition");
                        return false;
                    }

                    bool have_then_or_else = false;

                    // at i + 1 we either have THEN OR ELSE we could accept a statement..... 
                    idx++;
                    if (idx <= idx_max && data.Tokens[idx].Item1 == BlastScriptToken.Then)
                    {
                        have_then_or_else = true;
                        idx++;
                        // skip the closure 
                        if (!skip_closure(data, ref idx, idx_max)) return false;
                    }
                    if (idx <= idx_max && data.Tokens[idx].Item1 == BlastScriptToken.Else)
                    {
                        have_then_or_else = true;
                        idx++;
                        // skip the closure 
                        if (!skip_closure(data, ref idx, idx_max)) return false;
                    }

                    // check if we either have a then or else 
                    if (!have_then_or_else)
                    {
                        data.LogError("parser.FindNextStatement: malformed IF statement, expecting a THEN and/or ELSE compound");
                        return false;
                    }

                    // set the end of the statement and advance to next token 
                    i2 = idx;

                    // take any ; with this statement
                    while (idx <= idx_max && data.Tokens[idx].Item1 == BlastScriptToken.DotComma)
                    {
                        idx++;
                    }

                    if (idx == idx_max && data.Tokens[idx].Item1 == BlastScriptToken.CloseParenthesis)
                    {
                        // read past end 
                        idx++;
                    }

                    // found: ifthenelse statement between i1 and i2 with idx directly after it 
                    return true;

                    // for loops have the same form: [while/for](compound)(compound)
                case BlastScriptToken.For: 
                case BlastScriptToken.While:
                    idx++;

                    // skip the while condition closure 
                    if (!skip_closure(data, ref idx, idx_max)) return false;

                    // next should be a new closure or a single statement 
                    if (data.Tokens[idx].Item1 == BlastScriptToken.OpenParenthesis)
                    {
                        // TODO : could allow single statement 
                        if (!skip_closure(data, ref idx, idx_max)) return false;

                        // set the end of the statement and advance to next token 
                        i2 = idx;

                        // take any ; with this statement
                        while (idx <= idx_max && data.Tokens[idx].Item1 == BlastScriptToken.DotComma)
                        {
                            idx++;
                        }

                        if (idx == idx_max && data.Tokens[idx].Item1 == BlastScriptToken.CloseParenthesis)
                        {
                            // read past end 
                            idx++;
                        }

                        // found the while
                        return true;
                    }
                    else
                    {
                        // single statement 
                        data.LogError("find_next_statement: single statement in FOR/WHILE not supported yet use a closure");
                        data.LogToDo("find_next_statement: single statement in FOR/WHILE not supported yet use a closure");
                        return false;
                    }

                // switch(a > b)
                //(
                // case 1:
                //  (
                // e = result_1;  
                //  )
                //  default:
                //  (
                // e = result_2;
                //  )
                //);
                case BlastScriptToken.Switch:
                    idx++;

                    // skip the switch condition closure 
                    if (!skip_closure(data, ref idx, idx_max)) return false;

                    // skip over cases
                    if (!skip_closure(data, ref idx, idx_max))
                    {
                        data.LogError("find_next_statement: failed to scan over switch case/default closure");
                        return false;
                    }
                    else
                    {
                        i2 = idx;

                        // take any ; with this statement
                        while (idx <= idx_max && data.Tokens[idx].Item1 == BlastScriptToken.DotComma)
                        {
                            idx++;
                        }

                        return true;
                    }
            }

            // not found 
            return false;
        }


        //
        // statements:
        //
        // identifier; 
        // identifier();
        // identifier[3].x;
        // identifier = identifier op function;
        // 
        // if( statements ) then ( statements ) else ( statements );
        // while (statements ) then (statements);
        // switch(statement) case: [statement;]* default: [statement]*
        //
        node parse_statement(IBlastCompilationData data, int idx_start, in int idx_end)
        {
            int i, idx_condition;

            // determine statement type and process accordingly 
            switch (data.Tokens[idx_start].Item1)
            {
                // either an assignment or a function call with no use for result
                case BlastScriptToken.Identifier:

                    // an assignment is a sequence possibly containing compounds ending with ; or eof
                    return parse_sequence(data, ref idx_start, idx_end);

                case BlastScriptToken.If:
                    {
                        node n_if = new node(nodetype.ifthenelse, BlastScriptToken.If);
                        // parse a statement in the form:
                        //
                        //  if( condition-sequence ) then ( statement list ) else (statement list);
                        //
                        int idx_if = idx_start, idx_then, idx_else;

                        // scan to: then  
                        if (!find_next(data, BlastScriptToken.Then, idx_start, idx_end, out idx_then, true, false))
                        {
                            data.LogError($"parser.parse_statement: failed to locate matching THEN for IF statement found at token {idx_start} in statement from {idx_start} to {idx_end}");
                            return null;
                        }

                        // get IF condition sequence
                        i = idx_if + 1;
                        node n_condition = parse_sequence(data, ref i, idx_then - 1);
                        if (n_condition == null || !n_condition.HasChildNodes)
                        {
                            data.LogError($"parser.parse_statement: failed to parse IF condition or empty condition in statement from {idx_start} to {idx_end}");
                            return null;
                        }
                        // force node to be a condition, validation should later trip on it if its not 
                        n_condition.type = nodetype.condition;
                        n_condition.identifier = node.GenerateUniqueId("ifcond");
                        n_if.SetChild(n_condition);

                        // get THEN compound 
                        // - if first token: ( then compound 
                        // - if first token: IF then nested if then else ........     todo for now force it to be a compound
                        // - else simple sequence
                        if (data.Tokens[idx_then + 1].Item1 == BlastScriptToken.OpenParenthesis)
                        {
                            // get matching parenthesis 
                            if (find_next(data, BlastScriptToken.CloseParenthesis, idx_then + 2, idx_end, out i, true, false))
                            {
                                // parse statement list between the IFTHEN() 
                                node n_then = n_if.CreateChild(nodetype.ifthen, BlastScriptToken.Then, "then");
                                n_then.identifier = node.GenerateUniqueId("ifthen");
                                int exitcode = parse_statements(data, n_then, idx_then + 2, i);
                                if (exitcode != (int)BlastError.success)
                                {
                                    data.LogError($"parser.parse_statement: failed to parse IFTHEN statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                    return null;
                                }
                            }
                            else
                            {
                                // could not find closing )
                                data.LogError($"parser.parse_statement: failed to parse IFTHEN statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for IFTHEN closure");
                                return null;
                            }
                        }
                        else
                        {
                            // no IFTHEN () 
                            data.LogError($"parser.parse_statement: failed to parse IFTHEN statement list in statement from {idx_start} to {idx_end}, the statement list inside the THEN closure should be encapsulated by parenthesis.");
                            return null;
                        }

                        // get ELSE, by forcing THEN to have () we can get ELSE simply by skipping compound on search
                        idx_else = i + 1; // token after current must be ELSE for an else statement to be correct

                        if (idx_else <= idx_end && data.Tokens[idx_else].Item1 == BlastScriptToken.Else)
                        {
                            // read else, forse IFELSE with ()  
                            if (idx_else + 1 <= idx_end && data.Tokens[idx_else + 1].Item1 == BlastScriptToken.OpenParenthesis)
                            {
                                // get matching parenthesis 
                                if (find_next(data, BlastScriptToken.CloseParenthesis, idx_else + 2, idx_end, out i, true, false))
                                {
                                    // parse statement list between the IFTHEN() 
                                    node n_else = n_if.CreateChild(nodetype.ifelse, BlastScriptToken.Else, "else");
                                    n_else.identifier = node.GenerateUniqueId("ifelse");
                                    int exitcode = parse_statements(data, n_else, idx_else + 2, i);
                                    if (exitcode != (int)BlastError.success)
                                    {
                                        data.LogError($"parser.parse_statement: failed to parse IFELSE statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                        return null;
                                    }
                                }
                                else
                                {
                                    // could not find closing )
                                    data.LogError($"parser.parse_statement: failed to parse IFTHENELSE statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for IFELSE closure");
                                    return null;
                                }
                            }
                            else
                            {
                                // no IFTHEN () 
                                data.LogError($"parser.parse_statement: failed to parse IFTHENELSE statement list in statement from {idx_start} to {idx_end}, the statement list inside the ELSE closure should be encapsulated by parenthesis.");
                                return null;
                            }
                        }
                        return n_if;
                    }

                case BlastScriptToken.While:
                    {
                        node n_while = new node(nodetype.whileloop, BlastScriptToken.While);
                        n_while.identifier = node.GenerateUniqueId("while"); 

                        // next token MUST be ( 
                        i = idx_start + 1;
                        if (data.Tokens[i].Item1 == BlastScriptToken.OpenParenthesis)
                        {
                            // get matching parenthesis 
                            idx_condition = i + 1;
                            if (find_next(data, BlastScriptToken.CloseParenthesis, idx_condition, idx_end, out i, true, false))
                            {
                                // from here read the while condition sequence 
                                node n_condition = parse_sequence(data, ref idx_condition, i - 1);
                                if (n_condition == null || !n_condition.HasChildNodes)
                                {
                                    data.LogError($"parser.parse_statement: failed to parse WHILE condition or empty condition in statement from {idx_start} to {idx_end}");
                                    return null;
                                }
                                else
                                {
                                    n_condition.identifier = node.GenerateUniqueId("while_condition");  
                                    n_condition.type = nodetype.condition;
                                    n_while.SetChild(n_condition);
                                }
                            }
                            else
                            {
                                // could not find closing )
                                data.LogError($"parser.parse_statement: failed to parse WHILE statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for WHILE condition");
                                return null;
                            }

                            // now read while statement list
                            i = i + 1;
                            if (data.Tokens[i].Item1 == BlastScriptToken.OpenParenthesis)
                            {
                                // should have matching statement list 
                                int idx_compound = i + 1;
                                if (find_next(data, BlastScriptToken.CloseParenthesis, idx_compound, idx_end, out i, true, false))
                                {
                                    // parse statement list between the IFTHEN() 
                                    node n_compound = n_while.CreateChild(nodetype.whilecompound, BlastScriptToken.While, node.GenerateUniqueId("while_compound"));
                                    int exitcode = parse_statements(data, n_compound, idx_compound, i - 1);
                                    if (exitcode != (int)BlastError.success)
                                    {
                                        data.LogError($"parser.parse_statement: failed to parse WHILE statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                        return null;
                                    }
                                }
                                else
                                {
                                    // could not find closing )
                                    data.LogError($"parser.parse_statement: failed to parse WHILE statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for WHILE compound closure");
                                    return null;
                                }
                            }
                            else
                            {
                                // no WHILE compound () 
                                data.LogToDo("Allow while statements to omit () on compound statement list if 1 statement");
                                data.LogError($"parser.parse_statement: failed to parse WHILE statement in statement from {idx_start} to {idx_end}, the while compound statement list must be encapsulated in parenthesis.");
                                return null;
                            }
                        }
                        else
                        {
                            // no WHILE condition () 
                            data.LogError($"parser.parse_statement: failed to parse WHILE statement in statement from {idx_start} to {idx_end}, the while condition must be encapsulated in parenthesis.");
                            return null;
                        }

                        return n_while;
                    }

                case BlastScriptToken.For:
                    {
                        node n_for = new node(nodetype.forloop, BlastScriptToken.For);
                        n_for.identifier = node.GenerateUniqueId("for");
                        i = idx_start + 1;
                        if (data.Tokens[i].Item1 == BlastScriptToken.OpenParenthesis)
                        {
                            // get matching parenthesis 
                            idx_condition = i + 1;
                            if (find_next(data, BlastScriptToken.CloseParenthesis, idx_condition, idx_end, out i, true, false))
                            {
                                // should be 3 ; seperated statements 
                                int exitcode = parse_statements(data, n_for, idx_condition, i - 1);
                                if (exitcode != (int)BlastError.success)
                                {
                                    data.LogError($"Parser.ParseStatement: failed to parse FOR statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                    return null;
                                }

                                // should be 3 statements... we wont allow omitting one
                                if (n_for.children.Count != 3)
                                {
                                    data.LogError($"Parser.ParseStatement: failed to read FOR statement, found {n_for.children.Count} statements from token {idx_condition} to {i - 1} but expeced 3");
                                }
                            }
                            else
                            {
                                // could not find closing )
                                data.LogError($"Parser.ParseStatement: failed to parse FOR condition statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for FOR conditions");
                                return null;
                            }

                            // get the compound 
                            i = i + 1;
                            if (data.Tokens[i].Item1 == BlastScriptToken.OpenParenthesis)
                            {
                                // should have matching statement list 
                                int idx_compound = i + 1;
                                if (find_next(data, BlastScriptToken.CloseParenthesis, idx_compound, idx_end, out i, true, false))
                                {
                                    // parse statement list between the IFTHEN() 
                                    node n_compound = n_for.CreateChild(nodetype.compound, BlastScriptToken.While, node.GenerateUniqueId("for_compound"));
                                    int exitcode = parse_statements(data, n_compound, idx_compound, i - 1);
                                    if (exitcode != (int)BlastError.success)
                                    {
                                        data.LogError($"Parser.ParseStatement: failed to parse FOR statement list in statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                        return null;
                                    }
                                }
                                else
                                {
                                    // could not find closing )
                                    data.LogError($"parser.parse_statement: failed to parse FOR statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for FOR compound closure");
                                    return null;
                                }
                            }
                            else
                            {
                                // no WHILE compound () 
                                data.LogToDo("Allow FOR statements to omit () on compound statement list if 1 statement");
                                data.LogError($"Parser.ParseStatement: failed to parse FOR statement in statement from {idx_start} to {idx_end}, the FOR compound statement list must be encapsulated in parenthesis.");
                                return null;
                            }

                            return n_for;
                        }
                        else
                        {
                            data.LogError($"Parser.ParseStatement: failed to parse FOR statement, the for is not followed by ( but by an unsupported token: '{data.Tokens[i].Item1}'");
                            return null;
                        }
                    }


                case BlastScriptToken.Switch:
                    {
                        node n_switch = new node(nodetype.switchnode, BlastScriptToken.Switch);

                        // next token MUST be ( 
                        i = idx_start + 1;
                        if (data.Tokens[i].Item1 != BlastScriptToken.OpenParenthesis)
                        {
                            data.LogError($"parser.parse_statement: failed to parse switch statement in statement from {idx_start} to {idx_end}, the switch condition must be encapsulated in parenthesis.");
                            return null;
                        }

                        idx_condition = i + 1;
                        if (find_next(data, BlastScriptToken.CloseParenthesis, idx_condition, idx_end, out i, true, false))
                        {
                            // from here read the switch condition sequence 
                            node n_condition = parse_sequence(data, ref idx_condition, i - 1);
                            if (n_condition == null || !n_condition.HasChildNodes)
                            {
                                data.LogError($"parser.parse_statement: failed to parse switch condition or empty condition in statement from {idx_start} to {idx_end}");
                                return null;
                            }
                            else
                            {
                                n_condition.type = nodetype.condition;
                                n_condition.identifier = node.GenerateUniqueId("ifcond");
                                n_switch.SetChild(n_condition);
                            }
                        }
                        else
                        {
                            // could not find closing )
                            data.LogError($"parser.parse_statement: failed to parse SWITCH statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for SWITCH condition");
                            return null;
                        }

                        // read switch cases
                        idx_start = i + 1;
                        if (data.Tokens[idx_start].Item1 != BlastScriptToken.OpenParenthesis)
                        {
                            data.LogError($"parser.parse_statement: malformed parentheses in switch compound from {idx_start} to {idx_end}");
                            return null;
                        }

                        // find matching end of compound 
                        if (find_next(data, BlastScriptToken.CloseParenthesis, idx_start + 1, idx_end, out i, true, false))
                        {
                            int n_cases = 0;

                            // read all cases + default
                            while (true)
                            {
                                int idx_case;
                                idx_start = idx_start + 1;

                                // make sure there is at least 1 case or 1 default 
                                if (find_next(data, new BlastScriptToken[] { BlastScriptToken.Case, BlastScriptToken.Default }, idx_start, idx_end, out idx_case))
                                {
                                    // case or default ? 
                                    bool is_default = data.Tokens[idx_case].Item1 == BlastScriptToken.Default;
                                    bool is_case = data.Tokens[idx_case].Item1 == BlastScriptToken.Case;
                                    if (!(is_default || is_case))
                                    {
                                        data.LogError($"parser.parse_statement: failed to locate case or default statements in switch compound from {idx_start} to {idx_end}");
                                        return null;
                                    }
                                    n_cases = n_cases + 1;

                                    // read the b statements for the case/default 

                                    // here there must be an identifier sequence followed by :
                                    // directly after the case/default we must find ':' followed by either 1 statement or a compound with multiple statements 
                                    int idx_ternary;
                                    idx_case = idx_case + 1;
                                    if (!find_next(data, BlastScriptToken.TernaryOption, idx_case, idx_end, out idx_ternary, true, false))
                                    {
                                        data.LogError($"parser.parse_statement: failed to parse switch statement, malformed case/default compound from {idx_start} to {idx_end}");
                                        return null;
                                    }

                                    node n_case = new node(null);
                                    n_case.type = is_default ? nodetype.switchdefault : nodetype.switchcase;
                                    n_case.token = is_default ? BlastScriptToken.Default : BlastScriptToken.Case;
                                    n_switch.SetChild(n_case);

                                    if (!is_default)
                                    {
                                        node n_case_condition = parse_sequence(data, ref idx_case, idx_ternary - 1);
                                        n_case_condition.type = nodetype.condition;
                                        n_case.SetChild(n_case_condition);
                                    }

                                    // either 1 statement or a list surrounded in a compound. 
                                    if (data.Tokens[idx_ternary + 1].Item1 == BlastScriptToken.OpenParenthesis)
                                    {
                                        // a list in compound 
                                        idx_case = idx_ternary + 2;
                                        if (find_next(data, BlastScriptToken.CloseParenthesis, idx_case, idx_end, out i, true, false))
                                        {
                                            // parse statement list in the CASE: ()  
                                            int exitcode = parse_statements(data, n_case, idx_case, i - 1);
                                            if (exitcode != (int)BlastError.success)
                                            {
                                                data.LogError($"parser.parse_statement: failed to parse CASE/DEFAULT statement list in SWITCH statement from {idx_start} to {idx_end}, exitcode: {exitcode}");
                                                return null;
                                            }
                                            else
                                            {
                                                idx_start = i;
                                            }
                                        }
                                        else
                                        {
                                            // could not find closing )
                                            data.LogError($"parser.parse_statement: failed to parse CASE/DEFAULT statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for CASE compound closure");
                                            return null;
                                        }

                                    }
                                    else
                                    {
                                        // a list until the next case / default 
                                        data.LogToDo("single statement switch case / default ");
                                        data.LogError($"parser.parse_statement: failed to parse CASE/DEFAULT statement list in statement from {idx_start} to {idx_end}, could not locate closing parenthesis for CASE compound closure");
                                        return null;
                                    }
                                }
                                else
                                {
                                    break;
                                }
                            }

                            // verify: need at least 1 case or 1 default 
                            if (n_cases <= 0)
                            {
                                data.LogError("parser.parse_statement: found switch statement without case or default compound, this is not allowed");
                                return null;
                            }

                            return n_switch;
                        }
                        else
                        {
                            data.LogError($"parser.parse_statement: malformed parentheses in switch compound from {idx_start} to {idx_end}, could not locate closing parenthesis");
                            return null;
                        }
                    }
            }

            return null;
        }


        //
        // identifiers: 
        // 
        //      Identifier,     // [a..z][0..9|a..z]*[.|[][a..z][0..9|a..z]*[]]
        //      Indexer,        // .        * using the indexer on a numeric will define its fractional part 
        //      IndexOpen,      // [
        //      IndexClose,     // ]
        //
        //
        //    23423.323
        //    1111
        //    a234.x
        //    a123[3].x
        //    a123[a - 3].x[2].x
        //
        //    * in a later stage : (1 2 3 4).x          => vector with index 
        //    * thus we must also allow an indexer after compounds and functions
        //
        // * whitespace is allowed in between indexers when not numeric 
        // * parser should parse identifier into compounds if needed (flatten should then flatten that later :p)



        /// <summary>
        /// scan and parse a numeric from the token list in the form:
        /// 
        /// -100.23
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx">indext to start scan from, on success wil be at position after last token of identifier</param>
        /// <param name="idx_max">the max index to scan into</param>
        /// <returns>null on failure, a node with the value on success</returns>
        unsafe node scan_and_parse_numeric(IBlastCompilationData data, ref int idx, in int idx_max)
        {
            if (idx == idx_max && idx < data.Tokens.Count && data.Tokens[idx].Item1 == BlastScriptToken.Identifier)
            {
                idx++; 
                return new node(null)
                {
                    is_constant = true,
                    identifier = data.Tokens[idx-1].Item2,
                    type = nodetype.parameter
                };
            }

            // we wont ever have minus = true in current situation because of tokenizer and how we read assignments
            // but leave it here just in case 
            bool minus = data.Tokens[idx].Item1 == BlastScriptToken.Substract;
            

            bool has_data = !minus; // if first token is minus sign then we dont have data yet
            bool has_fraction = false;
            bool has_indexer = false;
            
            int vector_size = 1;

            string value = data.Tokens[idx].Item2;
            idx++;

            while (idx <= idx_max && idx < data.Tokens.Count)
            {
                switch (data.Tokens[idx].Item1)
                {
                    case BlastScriptToken.Identifier:
                        {
                            if (!has_data)
                            {
                                // first part of value
                                value += data.Tokens[idx].Item2;
                                idx++;
                                has_data = true;
                                break;
                            }

                            //if (has_indexer || (idx == idx_max))
                            {
                                if (has_data && !has_fraction && has_indexer)
                                {
                                    // last part of value
                                    value += data.Tokens[idx].Item2;
                                    has_fraction = true;
                                    idx++;
                                    // retrn a 'parameter' node... bad name.. 
                                    return new node(null)
                                    {
                                        is_constant = true,
                                        identifier = value,
                                        vector_size = 1, 
                                        type = nodetype.parameter
                                    };
                                }
                            }

                            //
                            // not a valid numeric  OR  a vector 
                            //
                            // - could grow vector here
                            //
                            if(has_data)
                            {
                                // todo, not sure - COULD GROW VECTOR HERE AND RETURN THAT IN NODE
                                return new node(null)
                                {
                                    is_constant = true,
                                    identifier = value,
                                    vector_size = vector_size, 
                                    type = nodetype.parameter
                                };
                            }

                            //idx++;

                            data.LogError($"scan_and_parse_numeric: sequence of operations not valid for a numeric value: {data.Tokens[idx].Item2} in section {idx} - {idx_max} => {Blast.VisualizeTokens(data.Tokens, idx, idx_max)}");
                            return null;

                            // break;
                        }

                    default:
                    case BlastScriptToken.Nop:
                        // ok after whole number and after number with indexer and fraction
                        if (has_data && !has_fraction && !has_indexer)
                        {
                            // ok, return value, a fractional would have returned in the above case
                            // idx++; dont inrease index, we stop on the next char after identifier 
                            return new node(null)
                            {
                                is_constant = true,
                                identifier = value,
                                vector_size = vector_size,
                                type = nodetype.parameter
                            };
                        }

                        data.LogError("scan_and_parse_numeric: sequence of operations not valid for a numeric value, whitespace is not allowed in the fractional part");
                        return null;

                    case BlastScriptToken.Indexer:
                        if (has_data && !has_indexer)
                        {
                            value = value + ".";
                            has_indexer = true;
                            idx++;
                            break;
                        }
                        data.LogError("scan_and_parse_numeric: sequence of operations not valid for a numeric value, error defining fractional part");
                        return null;
                }
            }

            // if we at this point have data (and only data) then return it 
            if (has_data && !has_fraction && !has_indexer)
            {
                return new node(null)
                {
                    is_constant = true,
                    identifier = value,
                    vector_size = vector_size,
                    type = nodetype.parameter
                };
            }

            // all other cases are an error
            data.LogError("scan_and_parse_numeric: sequence of operations not valid for a numeric value, error defining fractional part");
            return null;
        }

        /// <summary>
        /// scan input from idx building up 1 identifier as we go, after returning a valid node 
        /// the scan index will be on the token directly after the identifier 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx">the index starting the scan from and the must be on the first token of the identifier to parse. on succes it will be on the token directly after the identifier</param>
        /// <param name="idx_max">the max index to scan into (max including)</param>
        /// <returns>null on errors, a valid node on success
        /// - can return nodes: function() and identifier[34].x
        /// </returns>
        node scan_and_parse_identifier(IBlastCompilationData data, ref int idx, in int idx_max, bool add_minus = false)
        {
            // step 1> we read a number if

            // - first char is number 
            // - first token is negative sign second token first char is digit 
            bool minus = data.Tokens[idx].Item1 == BlastScriptToken.Substract;

            if(add_minus) 
            {
                // toggle minus sign if requested 
                minus = !minus;
            }

            bool is_number =
                // first minus and second char is digit 
                (minus && (idx + 1 <= idx_max) && data.Tokens[idx + 1].Item2 != null && data.Tokens[idx + 1].Item2.Length > 0 && char.IsDigit(data.Tokens[idx + 1].Item2[0]))
                ||
                // first char is digit 
                (data.Tokens[idx].Item2 != null && data.Tokens[idx].Item2.Length > 0 && char.IsDigit(data.Tokens[idx].Item2[0]));

            if (is_number)
            {
                // just return a numeric constant
                node n_var_or_constant = scan_and_parse_numeric(data, ref idx, in idx_max);
                
                if(n_var_or_constant != null && minus)
                {
                    // add back the negative sign, in a later stage it will get stripped when determining wether to load value from constants 
                    n_var_or_constant.identifier = "-" + n_var_or_constant.identifier;
                }
                return n_var_or_constant; 
            }


            // step 2> create identifier from tokens 
            ScriptFunctionDefinition function = data.Blast.GetFunctionByName(data.Tokens[idx].Item2);
            if (function != null)
            {
                // parse out function
                node n_function = scan_and_parse_function(data, function, ref idx, in idx_max);

                return NegateNodeInCompound(minus, n_function);
            }
            else
            {
                // now n_id should end up containing function and possible an index chain 
                node n_id = new node(null);
                n_id.identifier = data.Tokens[idx].Item2;
                n_id.type = nodetype.parameter;
                idx++;

                // grow a chain of indices 
                return NegateNodeInCompound(minus, grow_index_chain(data, n_id, ref idx, in idx_max));
            }
        }

        /// <summary>
        /// if minus:
        /// - insert a parent compound: parent.function => parent.compound.function 
        /// - insert sibling with substract opcode
        /// </summary>
        /// <param name="minus"></param>
        /// <param name="n_function"></param>
        /// <returns></returns>
        private static node NegateNodeInCompound(bool minus, node n_function)
        {
            // negate function result ? 
            if (minus)
            {
                // set a compound as parent 
                n_function.InsertParent(new node(nodetype.compound, BlastScriptToken.Nop));

                // add a substract op 
                n_function.InsertChild(0, new node(nodetype.operation, BlastScriptToken.Substract));

                // return the compound
                return n_function.parent;
            }
            else
            {
                // return the function node 
                return n_function;
            }
        }

        /// <summary>
        /// scan and parse out the next function. 
        /// 
        ///  function;
        ///  function();
        ///  function(function(a, b), c); 
        ///  function(function(a, b)[2].x, c); 
        /// 
        /// </summary>
        /// <param name="data">general compiler data</param>
        /// <param name="function">function to parse</param>
        /// <param name="idx">idx, starting at function, ending directly after</param>
        /// <param name="idx_max">max index to scan into</param>
        /// <returns>a node containing the function or null on failure</returns>
        node scan_and_parse_function(IBlastCompilationData data, ScriptFunctionDefinition function, ref int idx, in int idx_max)
        {
            node n_function = new node(null)
            {
                type = nodetype.function,
                identifier = function.Match,
                function = function
            };

            if (n_function.function != null)
            {
                // set vector type for known functions 
                switch ((ReservedScriptFunctionIds)n_function.function.FunctionId)
                {
                    case ReservedScriptFunctionIds.Pop2:
                        n_function.SetIsVector(2);
                        break;
                    case ReservedScriptFunctionIds.Pop3:
                        n_function.SetIsVector(3);
                        break;
                    case ReservedScriptFunctionIds.Pop4:
                        n_function.SetIsVector(4);
                        break;
                }
            }

            // parse parameters, which possibly are statements too
            // idx should be at ( directly after function, any other token then ,; is an error. 

            idx++;
            bool parameter_less = false;

            // end reached? parameterless function 
            if (idx < idx_max)
            {
                if (data.Tokens[idx].Item1 != BlastScriptToken.OpenParenthesis)
                {
                    if (data.Tokens[idx].Item1 == BlastScriptToken.DotComma
                        ||
                        data.Tokens[idx].Item1 == BlastScriptToken.Comma)
                    {
                        // parameter less function 
                        idx++;
                        parameter_less = true;
                    }
                    else
                    {
                        // error condition 
                        data.LogError($"parse.parse_function: scanned function {function.Match}, malformed parameter compound, expecting parameters, empty compound or statement termination");
                        return null;
                    }
                }
            }
            else
            {
                parameter_less = true;
                idx++;
            }

            // scan for parameters and indexer
            if (!parameter_less)
            {
                int idx_end;

                // at '(', find next ) skipping over compounds
                idx++;
                if (find_next(data, BlastScriptToken.CloseParenthesis, idx, idx_max, out idx_end, true, false))
                {
                    int end_function_parenthesis = idx_end;
                    List<node> current_nodes = new List<node>(); 

                    while (idx < end_function_parenthesis)
                    {
                        BlastScriptToken token = data.Tokens[idx].Item1;
                        switch (token)
                        {
                            // identifier, possibly a nested function 
                            case BlastScriptToken.Identifier:
                                current_nodes.Add(n_function.SetChild(scan_and_parse_identifier(data, ref idx, idx_end)));
                                break;

                            // seperator 
                            case BlastScriptToken.Comma:
                            case BlastScriptToken.Nop:

                                if (current_nodes.Count > 0)
                                {
                                    if (current_nodes.Count > 1)
                                    {
                                        // create a child node with the current nodes as children 
                                        // foreach (node cnc in current_nodes) n_function.children.Remove(cnc); // this is not needed
                                        n_function.CreateChild(nodetype.compound, BlastScriptToken.Nop, "").SetChildren(current_nodes);
                                    }
                                    else
                                    {
                                       // it is already a child in this case and nothing needs to happen
                                    }

                                    current_nodes.Clear(); 
                                }
                                idx++;
                                break;

                            // compounds embedded in parameters -> statements resulting in values : 'sequences'
                            case BlastScriptToken.OpenParenthesis:

                                if(data.Tokens[idx - 1].Item1 == BlastScriptToken.CloseParenthesis)
                                {
                                    if (current_nodes.Count > 0)
                                    {
                                        //
                                        // this is a sequence within the parameter sequence 
                                        //

                                        // it is either a list of operations or a vector, in both case push it
                                        n_function.CreateChild(nodetype.compound, BlastScriptToken.Nop, "").SetChildren(current_nodes);
                                        current_nodes.Clear();
                                    }
                                }

                                if (find_next(data, BlastScriptToken.CloseParenthesis, idx + 1, idx_max, out idx_end, true, false))
                                {
                                    current_nodes.Add(n_function.SetChild(parse_sequence(data, ref idx, idx_end)));
                                }
                                else
                                {
                                    data.LogError($"parser.parse_function: malformed compounded statement in function parameter list, found openparenthesis at tokenindex {idx+1} but no matching close before {idx_max}");
                                    return null;
                                }
                                break;

                            // operations
                            case BlastScriptToken.Add:
                            case BlastScriptToken.Substract:
                            case BlastScriptToken.Divide:
                            case BlastScriptToken.Multiply:
                            case BlastScriptToken.Equals:
#if SUPPORT_TERNARY
                            case BlastScriptToken.Ternary:
                            case BlastScriptToken.TernaryOption:
#endif
                            case BlastScriptToken.SmallerThen:
                            case BlastScriptToken.GreaterThen:
                            case BlastScriptToken.SmallerThenEquals:
                            case BlastScriptToken.GreaterThenEquals:
                            case BlastScriptToken.NotEquals:
                            case BlastScriptToken.And:
                            case BlastScriptToken.Or:
                            case BlastScriptToken.Xor:
                            case BlastScriptToken.Not:
                                // we are scanning inside a parameter list and are dropping the comma;s   (somewhere)
                                // the minus should be added to the next identifier before the next comma
                                current_nodes.Add(n_function.CreateChild(nodetype.operation, token, data.Tokens[idx].Item2));
                                idx++;
                                break;

                            case BlastScriptToken.CloseParenthesis:
                                // this is ok if we have a single compound in current_nodes representing a parameter 
                                if (current_nodes.Count == 1 && current_nodes[0].type == nodetype.compound)
                                {
                                    idx++;
                                }
                                else
                                {
                                    data.LogError($"parse.parse_function: found unexpected token '{token}' in function '{function.Match}' while parsing function parameters`, token index: {idx} => {Blast.VisualizeTokens(data.Tokens, idx, idx_end)}  => \n{n_function.ToNodeTreeString()}");
                                    return null;
                                }
                                break; 
                            case BlastScriptToken.Indexer:
                            case BlastScriptToken.IndexOpen:
                            case BlastScriptToken.IndexClose:
                            case BlastScriptToken.If:
                            case BlastScriptToken.Then:
                            case BlastScriptToken.Else:
                            case BlastScriptToken.While:
                            case BlastScriptToken.Switch:
                            case BlastScriptToken.Case:
                            case BlastScriptToken.Default:
                                data.LogError($"parse.parse_function: found unexpected token '{token}' in function '{function.Match}' while parsing function parameters`, token index: {idx} => {Blast.VisualizeTokens(data.Tokens, idx, idx_end)}  => \n{n_function.ToNodeTreeString()}");
                                return null;
                        }
                    }

                    //if we have nodes in this list, then we need to child them so we create a compounded param asif encountering a comma
                    if (current_nodes.Count > 0)
                    {
                        // create a child node with the current nodes as children 
                        // - create a compound if more then one child 
                        if (current_nodes.Count > 1)
                        {
                            n_function.CreateChild(nodetype.compound, BlastScriptToken.Nop, "").SetChildren(current_nodes);
                        }
                        else
                        {
                            n_function.SetChild(current_nodes[0]); 
                        }
                    }


                    idx = end_function_parenthesis + 1; 
                }
                else
                {
                    data.LogError($"parse.parse_function: scanned function {function.Match} has malformed parenthesis");
                }

                // validate what we can: determine parameter count from nr of seperators, other validations in later stages
                int param_count = n_function.children.Count;
                if (param_count < function.MinParameterCount || param_count > function.MaxParameterCount)
                {
                    //  fails in case: 
                    //  reads:          clamp((1 2), 3, 4)    
                    //  produces:       clamp(((1 2) 3) 4)
                    //  -> dueue to tokenizer removing the , after the )

                    data.LogError($"parser.parse_function: found function {function.Match} with {param_count} parameters while it should have {function.MinParameterCount} to {function.MaxParameterCount} parameters => \n{n_function.ToNodeTreeString()}");
                    return null;
                }

                // allow a chain of indices to follow
                return grow_index_chain(data, n_function, ref idx, in idx_max);
            }
            // Function without indexers or parameters 
            else
            {
                // validate function can be parameterless 
                if (function.MinParameterCount == 0)
                {
                    return n_function;
                }
                else
                {
                    data.LogError($"parser.parse_function: found function {function.Match} without parameters while at minimum {function.MinParameterCount} parameters are expected => \n{n_function.ToNodeTreeString()}");
                    return null;
                }
            }
        }

        /// <summary>
        /// parse a sequence of tokens between () into a list of nodes 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="idx">index directly after the opening ( or at first token of sequence within () </param>
        /// <param name="idx_max"></param>
        /// <returns></returns>
        node parse_sequence(IBlastCompilationData data, ref int idx, in int idx_max)
        {
            int idx_end = -1;
            node n_sequence = new node(null) { type = nodetype.compound };

            // if starting on parenthesis assume sequence compounded with parenthesis
            bool has_parenthesis = data.Tokens[idx].Item1 == BlastScriptToken.OpenParenthesis;
            if (has_parenthesis) idx++;

            BlastScriptToken prev_token = BlastScriptToken.Nop;
            bool minus = false;
            bool if_sure_its_vector_define = false; 

            // read all tokens 
            while (idx <= idx_max)
            {
                BlastScriptToken token = data.Tokens[idx].Item1;
                if (token == BlastScriptToken.CloseParenthesis)
                {
                    if (has_parenthesis || idx == idx_max)
                    {
                        // closing 
                        break;
                    }
                }

                if(prev_token == BlastScriptToken.Comma)
                {
                    if(token != BlastScriptToken.Substract)
                    {
                        // should we raise error ?  or just allow it TODO 
#if DEBUG
                        data.LogTrace(", used in non parameter sequence");
#endif
                    }
                }

                switch (token)
                {
                    // identifier, possibly a nested function 
                    case BlastScriptToken.Identifier:
                        
                        // this cannot be anything else when 2 identifiers follow 
                        if (prev_token == BlastScriptToken.Identifier) if_sure_its_vector_define = true; 

                        node ident = n_sequence.SetChild(scan_and_parse_identifier(data, ref idx, idx_max, minus));

                        // reset minus sign after reading an identifier 
                        minus = false; 
                        break;

                    case BlastScriptToken.Nop:
                        idx++;
                        break;

                    // seperator not allowed in sequence 
                    case BlastScriptToken.Comma:
                        
                        // only allow in sequence if sure its a vector define ??  
                        // data.LogError($"parser.parse_sequence: seperator token '{token}' not allowed inside a sequence");
                        idx++;
                        break; 

                    case BlastScriptToken.DotComma:
                        if (idx == idx_max)
                        {
                            // allow ; on end of of range 
                            return n_sequence;
                        }
                        else
                        {
                            data.LogError($"parser.parse_sequence: seperator token '{token}' not allowed inside a sequence");
                            return null;
                        }

                    // compounds embedded in parameters -> statements resulting in values : 'sequences'
                    case BlastScriptToken.OpenParenthesis:
                        if (find_next(data, BlastScriptToken.CloseParenthesis, idx + 1, idx_max, out idx_end, true, false))
                        {
                            node n_compound = parse_sequence(data, ref idx, idx_end);
                            if (n_compound != null)
                            {
                                n_sequence.SetChild(n_compound);
                                idx++;
                            }
                            else
                            {
                                data.LogError("parser.parse_sequence: error parsing nested sequence");
                                return null;
                            }
                        }
                        else
                        {
                            data.LogError("parser.parse_sequence: malformed nested sequence, failed to located closing parenthesis");
                            return null;
                        }
                        break;

                    // operations
                    case BlastScriptToken.Add:
                    case BlastScriptToken.Divide:
                    case BlastScriptToken.Multiply:
                    case BlastScriptToken.Equals:
#if SUPPORT_TERNARY
                    case BlastScriptToken.Ternary:
                    case BlastScriptToken.TernaryOption:
#endif
                    case BlastScriptToken.SmallerThen:
                    case BlastScriptToken.GreaterThen:
                    case BlastScriptToken.SmallerThenEquals:
                    case BlastScriptToken.GreaterThenEquals:
                    case BlastScriptToken.NotEquals:
                    case BlastScriptToken.And:
                    case BlastScriptToken.Or:
                    case BlastScriptToken.Xor:
                    case BlastScriptToken.Not:
                        n_sequence.CreateChild(nodetype.operation, token, data.Tokens[idx].Item2);
                        idx++;
                        break;

                    case BlastScriptToken.Substract:
                        // if its the first, then its NOT an operation
                        if (prev_token == BlastScriptToken.Nop
                            ||
                            // if the previous was a seperator 
                            prev_token == BlastScriptToken.Comma
                            || 
                            if_sure_its_vector_define)
                        {
                            // no an operation, but minus sign 
                            minus = true;
                            idx++;
                        }
                        else
                        {
                            // handle as a mathamatical operation 
                            n_sequence.CreateChild(nodetype.operation, token, data.Tokens[idx].Item2);
                            idx++;
                        }
                        break; 

                    case BlastScriptToken.CloseParenthesis:
                    case BlastScriptToken.Indexer:
                    case BlastScriptToken.IndexOpen:
                    case BlastScriptToken.IndexClose:
                    case BlastScriptToken.If:
                    case BlastScriptToken.Then:
                    case BlastScriptToken.Else:
                    case BlastScriptToken.While:
                    case BlastScriptToken.Switch:
                    case BlastScriptToken.Case:
                    case BlastScriptToken.Default:
                    default:
                        data.LogError($"parse.parse_sequence: found unexpected token '{token}' while parsing sequence");
                        return null;
                }

                prev_token = token;
            }

            return n_sequence;
        }

        /// <summary>
        /// grow a chain of indices after some other node/token
        /// </summary>
        /// <param name="data"></param>
        /// <param name="chain_root">node to index</param>
        /// <param name="idx">token index to start reading chain from</param>
        /// <param name="idx_max">max index to grow into</param>
        /// <returns>chain root node with chain as children, or null on failure</returns>
        node grow_index_chain(IBlastCompilationData data, node chain_root, ref int idx, in int idx_max)
        {
            // STEP 3> grow identifier indexing chain (functions can be indexed)
            node n_id = chain_root;
            bool has_open_indexer = false;
            bool last_is_dot_indexer = false;

            while (idx <= idx_max && idx < data.Tokens.Count)
            {
                switch (data.Tokens[idx].Item1)
                {
                    case BlastScriptToken.Indexer:
                        if (last_is_dot_indexer || has_open_indexer)
                        {
                            data.LogError("parser.grow_index_chain: double indexer found");
                            return null;
                        }
                        n_id = n_id.AppendIndexer(BlastScriptToken.Indexer, ".");
                        last_is_dot_indexer = true;
                        idx++;
                        continue;

                    case BlastScriptToken.IndexOpen:
                        if (last_is_dot_indexer || has_open_indexer)
                        {
                            data.LogError("parser.grow_index_chain: double indexer found");
                            return null;
                        }
                        n_id = n_id.AppendIndexer(BlastScriptToken.IndexOpen, "[");
                        last_is_dot_indexer = false;
                        has_open_indexer = true;
                        idx++;
                        continue;

                    case BlastScriptToken.IndexClose:
                        if (!has_open_indexer)
                        {
                            data.LogError("parser.grow_index_chain: indexer mismatch");
                            return null;
                        }
                        n_id = n_id.AppendIndexer(BlastScriptToken.IndexClose, "]");
                        has_open_indexer = false;
                        last_is_dot_indexer = false;
                        idx++;
                        continue;


                    case BlastScriptToken.Identifier:
                        if (has_open_indexer || last_is_dot_indexer)
                        {
                            data.LogToDo("parser.grow_index_chain - parse sequence on open indexers");
                            //
                            // ! possible sequence in open indexer 
                            // 
                            n_id = n_id.AppendIndexer(BlastScriptToken.Identifier, data.Tokens[idx].Item2);
                            idx++;
                            last_is_dot_indexer = false;
                        }
                        else
                        {
                            // not part of this identifier, done growing 
                            return chain_root;
                        }
                        break;

                    default:
                        if (last_is_dot_indexer)
                        {
                            data.LogError("parser.grow_index_chain: index identifier mismatch");
                            return null;
                        }

                        if (has_open_indexer)
                        {
                            data.LogToDo("parser.grow_index_chain -  sequence on open indexers");
                            //
                            // ! possible sequence in open indexer 
                            // 
                            n_id = n_id.AppendIndexer(data.Tokens[idx].Item1, data.Tokens[idx].Item2);
                            idx++;
                            last_is_dot_indexer = false;
                        }
                        else
                        {
                            // done growing identifier 
                            return chain_root;
                        }
                        break;
                }
            }

            // nothing after initial part within range 
            return chain_root;
        }

        /// <summary>
        /// check if the node is an assignment 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="node">node to map out</param>
        /// <returns></returns>
        bool check_if_assignment_node(IBlastCompilationData data, node ast_node)
        {
            // check if this is an assignment 
            // [identifier][indexchain] = [sequence] ;/nop
            if (ast_node.children.Count > 1)
            {
                // first child must be a non constant parameter 
                // the second must be an assignment operator 
                if (ast_node.children[0].type == nodetype.parameter
                    &&
                    ast_node.children[1].type == nodetype.operation && ast_node.children[1].token == BlastScriptToken.Equals)
                {
                    // looks like an assignment, check out the parameter. it must be non constant 
                    node param = ast_node.children[0];
                    if (param.variable == null)
                    {
                        param.variable = ((CompilationData)data).GetOrCreateVariable(param.identifier);
                    }
                    if (param.variable == null)
                    {
                        // failed to create variable  ?? 
                        data.LogError($"parser: failed to get or create variable '{param.identifier}'");
                    }
                    else
                    {
                        if (param.variable.IsConstant)
                        {
                            data.LogError($"parser: the subject of an assignment cannot be constant.");
                        }
                    }

                    // set node to be assignment type 
                    if (data.IsOK)
                    {
                        ast_node.type = nodetype.assignment;
                        // set the identifier of the assignee
                        ast_node.identifier = param.identifier;
                        ast_node.variable = param.variable;
                        // remove the first 2 child nodes, these are the id = and can now be omitted
                        ast_node.children.RemoveRange(0, 2);
                    }
                }
            }

            return data.IsOK;
        }

        /// <summary>
        /// parse a statement list
        /// - depending on defines this may execute multithreaded 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="parent">the parent node</param>
        /// <param name="idx_start">starting index into tokens </param>
        /// <param name="idx_max">max index in tokens to scan into</param>
        /// <returns>exitcode - blasterror</returns>
        int parse_statements(IBlastCompilationData data, node parent, int idx_start, int idx_max)
        {
            int idx_token = idx_start, idx_end = -1;

            List<int4> idx_statements = new List<int4>();

            // first scan&extract all statement indices from the token array 

            while (idx_token <= idx_max)
            {
                if (find_next_statement(data, ref idx_token, idx_max, out idx_start, out idx_end))
                {
                    idx_statements.Add(new int4(idx_start, idx_end, idx_statements.Count, 0));

                    if (idx_token == idx_max) break; 
                }
                else
                {
                    break;
                }
            }

            if (idx_statements.Count == 0 && idx_max > idx_start)
            {
                data.LogError("parser.parse_statements: failed to find complete statements in token list");
                return (int)BlastError.error;
            }

            node[] nodes = new node[idx_statements.Count];

            // if parallel enabled and running from root node then run multithreaded
            if (parent == data.AST && data.CompilerOptions.ParallelCompilation)
            {
                Parallel.ForEach(idx_statements, idx =>
                {
                    // parse the statement from the token array
                    node ast_node = parse_statement(data, idx[0], idx[1]);
                    if (ast_node != null)
                    {
                        nodes[idx[2]] = ast_node;
                       
                        // check if the parsed statement is an assignment 
                        check_if_assignment_node(data, ast_node);
                    }
                });
            }
            else
            {
                // single threaded debug friendly
                foreach (int4 idx in idx_statements)
                {
                    // parse the statement from the token array
                    node ast_node = parse_statement(data, idx[0], idx[1]);
                    if (ast_node != null)
                    {
                        nodes[idx[2]] = ast_node;

                        // check if the parsed statement is an assignment 
                        check_if_assignment_node(data, ast_node);

                        // get all function nodes 
                        // node[] functions = ast_node.GetChildren(nodetype.function);
                    }
                }
            }

            // check each is set, report errors otherwise
            foreach (int4 idx in idx_statements)
            {
                node ast_node = nodes[idx[2]];
                if (ast_node == null)
                {
                    data.LogError($"parser.parse_statements: failed to parse statement from token {idx[0]} to {idx[1]}");
                }
                
                // make it part of the ast tree
                parent.SetChild(ast_node);
            }

            if (data.IsOK)
            {
                return (int)BlastError.success;
            }
            else
            {
                return (int)BlastError.error;
            }
        }

        /// <summary>
        /// map identifiers to variables
        /// - determine if a opcode constant (1,2 , pi and stuff) or just a constant number on the data stack 
        /// </summary>
        /// <param name="data">compiler data</param>
        /// <param name="ast_node">the node to scan including child nodes</param>
        /// <returns>true if succeeded / IsOK</returns>
        bool map_identifiers(IBlastCompilationData data, node ast_node)
        {
            // if a parameter and not already mapped to something 
            if (ast_node.type == nodetype.parameter && ast_node.variable == null)
            {
                CompilationData cdata = data as CompilationData;

                // although the tokenizer recombines the minus sign with variables we split them here 
                // to lookup a possible match in constants, if matched to any the negation is put back
                // seperate as an operand before the positive constant, essentially undoing the work from tokenizer in the case of known constants 
                bool is_negated = ast_node.identifier[0] == '-';
                bool is_constant = is_negated || char.IsDigit(ast_node.identifier[0]);
                bool is_define = false;

                // check if its a define 
                string defined_value;
                if (!is_constant && data.TryGetDefine(ast_node.identifier, out defined_value))
                {
                    ast_node.identifier = defined_value;
                    is_define = true; 
                }

                // if its an input or output variable it will already exist
                if (!cdata.ExistsVariable(ast_node.identifier))
                {
                    if (is_define)
                    {
                        // reeval if define replaced identifier 
                        is_negated = ast_node.identifier[0] == '-';
                        is_constant = is_negated || char.IsDigit(ast_node.identifier[0]);
                    }

                    // if not known or constant raise undefined error
                    if (!is_constant)
                    {
                        data.LogError($"parser.map_identifiers: identifier '{ast_node.identifier}' is not constant and not defined in input and as such is undefined.");
                        return false;
                    }

                    // if we compile using system constant then use opcodes for known constants 
                    if (data.CompilerOptions.CompileWithSystemConstants)
                    {
                        
                        // its a constant, only lookup the positive part of values, if a match on negated we add back a minus op
                        string value = is_negated ? ast_node.identifier.Substring(1) : ast_node.identifier;

                        blast_operation op = data.Blast.GetConstantValueOperation(value, data.CompilerOptions.ConstantEpsilon);
                        if (op == blast_operation.nan)
                        {
                            data.LogError($"parse.map_identifiers: ({ast_node.identifier}) could not convert to a float or use as mathematical constant (pi, epsilon, infinity, minflt, euler)");
                            return false;
                        }
                        // get / create variable, reference count is updated accordingly 
                        if (is_constant && op != blast_operation.nop)
                        {
                            // dont create variables for constant ops 
                            ast_node.is_constant = true;
                            ast_node.constant_op = op;

                            // if the value was negated before matching then add the minus sign before it
                            // - this is cheaper then adding a full 32 bit value
                            // - this makes it more easy to see for analyzers if stuff is negated 
                            if (is_negated)
                            {
                                // put this node in a compound
                                // - effectively do this:  parent->child  ->  parent->compound->child
                                node compound = ast_node.InsertParent(new node(nodetype.compound, BlastScriptToken.Identifier));  
                                
                                // and replace it at parent 
                                ast_node.InsertBeforeThisNodeInParent(nodetype.operation, BlastScriptToken.Substract);
                                ast_node.identifier = value; // update to the non-negated value 
                            }

                            // we know its constant, if no other vectorsize is set force 1
                            if (ast_node.vector_size < 1) ast_node.vector_size = 1; 
                        }
                        else
                        {
                            // non op encoded constant, create a variable for it 
                            ast_node.variable = cdata.CreateVariable(ast_node.identifier);
                        }

                    }
                    else
                    {
                        // not compiling with system constants, just add a variable 
                        ast_node.variable = cdata.CreateVariable(ast_node.identifier);
                    }
                }
                else
                {
                    // get existing variable, updating reference count 
                    ast_node.variable = cdata.GetVariable(ast_node.identifier);
                    Interlocked.Increment(ref ast_node.variable.ReferenceCount);
                }
            }

            // map children (collection may change) 
            int i = 0; 
            while(i < ast_node.ChildCount)
            {
                int c = ast_node.ChildCount;
                map_identifiers(data, ast_node.children[i]);
                if(c != ast_node.ChildCount)
                {
                    // childlist changed 
                    i += ast_node.ChildCount - c;
                }
                i++;
            }
                
            return data.IsOK;
        }

        /// <summary>
        /// execute the parser stage:
        /// - parse tokens into node tree
        /// - map identifiers (indexers, functions, constants) 
        /// </summary>
        /// <param name="data">compilation data</param>
        /// <returns>exitcode, 0 == success</returns>
        public int Execute(IBlastCompilationData data)
        {
            // parse tokens 
            int exitcode = parse_statements(data, data.AST, 0, data.Tokens.Count - 1);

            // identify parameters 
            if (exitcode == (int)BlastError.success)
            {
                // walk each node, map each identifier as a variable or system constant
                // we start at the root and walk to each leaf, anything not known should raise an error 
                if (!map_identifiers(data, data.AST))
                {
                    data.LogError($"parser: failed to map all identifiers");
                    exitcode = (int)BlastError.error;
                }
            }
            return exitcode;
        }
    }

}